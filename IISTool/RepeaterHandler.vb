Imports System
Imports System.Net
Imports System.Net.Sockets
Imports System.Net.WebSockets
Imports System.Web
Imports System.Web.WebSockets
Imports System.Threading
Imports System.Threading.Tasks


Public Class RepeaterHandler
    Implements IHttpHandler

    Public Overridable ReadOnly Property IsReusable As Boolean Implements System.Web.IHttpHandler.IsReusable
        Get
            Return False
        End Get
    End Property

    Public Overridable Sub ProcessRequest(context As HttpContext) Implements IHttpHandler.ProcessRequest

        If Not context.IsWebSocketRequest Then

            context.Response.StatusCode = 400
            Return
        End If

        context.AcceptWebSocketRequest(
            Function(web_context) As Task

                Dim ext = context.Request.CurrentExecutionFilePathExtension
                Dim x = System.Configuration.ConfigurationManager.AppSettings(String.Format("repeater:{0}", If(ext.Length = 0, "*", ext.Substring(1))))

                Dim scheme = x.IndexOf(":"c)
                If scheme = -1 Then Throw New Exception("scheme not found")
                Select Case x.Substring(0, scheme).ToLower
                    Case "file" : Me.ProcessFile(web_context, x.Substring(scheme + 1))
                    Case "tcp" : Me.ProcessNetwork(web_context, AddressFamily.InterNetwork, ProtocolType.Tcp, x.Substring(scheme + 1))
                    Case "udp" : Me.ProcessNetwork(web_context, AddressFamily.InterNetwork, ProtocolType.Udp, x.Substring(scheme + 1))
                    Case "tcp6" : Me.ProcessNetwork(web_context, AddressFamily.InterNetworkV6, ProtocolType.Tcp, x.Substring(scheme + 1))
                    Case "udp6" : Me.ProcessNetwork(web_context, AddressFamily.InterNetworkV6, ProtocolType.Udp, x.Substring(scheme + 1))
                End Select

                Return Nothing
            End Function)

    End Sub

    Public Overridable Sub ProcessFile(context As AspNetWebSocketContext, file As String)

        ' exename arguments
        Dim sep = file.IndexOf(" "c)

        Dim p = New System.Diagnostics.Process
        p.StartInfo = New System.Diagnostics.ProcessStartInfo(file)
        p.StartInfo.FileName = If(sep < 0, file, file.Substring(0, sep))
        p.StartInfo.Arguments = If(sep < 0, "", file.Substring(sep + 1))
        p.StartInfo.UseShellExecute = False
        p.StartInfo.RedirectStandardInput = True
        p.StartInfo.RedirectStandardOutput = True
        p.StartInfo.RedirectStandardError = True
        p.StartInfo.CreateNoWindow = True
        p.StartInfo.WindowStyle = Diagnostics.ProcessWindowStyle.Hidden

        p.Start()

        Dim do_stdout = Task.Run(
          Async Function()
              Dim buffer(1024) As Byte
              Do While True

                  Dim count = p.StandardOutput.BaseStream.Read(buffer, 0, buffer.Length)
                  If count <= 0 Then Return
                  Await context.WebSocket.SendAsync(New ArraySegment(Of Byte)(buffer, 0, count), WebSocketMessageType.Binary, False, CancellationToken.None)
              Loop
          End Function)

        Dim do_stderr = Task.Run(
          Async Function()
              Dim buffer(1024) As Byte
              Do While True

                  Dim count = p.StandardError.BaseStream.Read(buffer, 0, buffer.Length)
                  If count <= 0 Then Return
                  Await context.WebSocket.SendAsync(New ArraySegment(Of Byte)(buffer, 0, count), WebSocketMessageType.Binary, False, CancellationToken.None)
              Loop
          End Function)

        Dim do_stdin = Task.Run(
          Async Function()
              Dim temp(1024) As Byte
              Dim buffer As New ArraySegment(Of Byte)(temp)
              Do While True

                  Dim result = Await context.WebSocket.ReceiveAsync(buffer, CancellationToken.None)
                  If result.Count <= 0 Then Return
                  p.StandardInput.BaseStream.Write(buffer.Array, 0, result.Count)
                  p.StandardInput.BaseStream.Flush()
              Loop
          End Function)

        p.WaitForExit()
    End Sub

    Public Overridable Sub ProcessNetwork(context As AspNetWebSocketContext, family As AddressFamily, protocol As Sockets.ProtocolType, ip As String)

        ' "port" or "hostname:port"
        Dim port = 0
        Dim sep = ip.IndexOf(":")
        If sep < 0 Then

            port = CInt(ip)
            ip = "localhost"
        Else
            port = CInt(ip.Substring(sep + 1))
            ip = ip.Substring(0, sep)
        End If

        Using sock As New Socket(family, SocketType.Stream, protocol)

            sock.Connect(ip, port)

            Dim receive_done = False
            Dim do_receive = Task.Run(
              Async Function()
                  Dim buffer(1024) As Byte
                  Do While True

                      Dim count = sock.Receive(buffer)
                      If count <= 0 Then

                          receive_done = True
                          Return
                      End If
                      Await context.WebSocket.SendAsync(New ArraySegment(Of Byte)(buffer, 0, count), WebSocketMessageType.Binary, False, CancellationToken.None)
                  Loop
              End Function)

            Dim send_done = False
            Dim do_send = Task.Run(
              Async Function()
                  Dim temp(1024) As Byte
                  Dim buffer As New ArraySegment(Of Byte)(temp)
                  Do While True

                      Dim result = Await context.WebSocket.ReceiveAsync(buffer, CancellationToken.None)
                      If result.Count <= 0 Then

                          send_done = True
                          Return
                      End If
                      sock.Send(buffer.Array, 0, result.Count, SocketFlags.None)
                  Loop
              End Function)

            Do While Not receive_done OrElse Not send_done

                Thread.Sleep(100L)
            Loop
        End Using

    End Sub
End Class
