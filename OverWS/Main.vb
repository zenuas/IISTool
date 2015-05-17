Imports System
Imports System.Collections.Generic
Imports System.Net
Imports System.Net.Sockets
Imports System.Net.Security
Imports System.Net.WebSockets
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Diagnostics


Public Class Main

    Public Shared Sub Main(args() As String)

        Dim self As New Main
        self.Run(CommandLineParser.Parse(self, args))
    End Sub

    Public Overridable Sub Run(args() As String)

        Dim listener As New TcpListener(IPAddress.Any, Me.LocalPort)
        listener.Start()
        Console.WriteLine("listen start")

        If Me.SSLValidationSkip Then

            ServicePointManager.ServerCertificateValidationCallback =
                Function(s, certificate, chain, sslPolicyError) As Boolean

                    Return True
                End Function
        End If

        Dim accept As AsyncCallback =
            New AsyncCallback(
                Async Sub(x As IAsyncResult)

                    If Not x.IsCompleted OrElse Not x.CompletedSynchronously Then x.AsyncWaitHandle.WaitOne()

                    Using local = listener.EndAcceptTcpClient(x)

                        Debug.WriteLine("accept {0}", local.Client.RemoteEndPoint)
                        listener.BeginAcceptTcpClient(accept, Nothing)

                        Using remote As New ClientWebSocket

                            If Me.ProxyHost.Length > 0 Then remote.Options.Proxy = New WebProxy(Me.ProxyHost, Me.ProxyPort)

                            Debug.WriteLine("connect {0} -> {1}", local.Client.RemoteEndPoint, Me.Uri)
                            Await remote.ConnectAsync(New Uri(Me.Uri), CancellationToken.None)
                            Dim local_stream = local.GetStream

                            Dim local_done = False
                            Dim local_buffer(1024) As Byte
                            Dim local_callback As AsyncCallback =
                                New AsyncCallback(
                                    Async Sub(xx As IAsyncResult)

                                        Try
                                            Dim count = local_stream.EndRead(xx)
                                            'console_write(local_buffer, count, String.Format("{0} -> {1}", local.Client.RemoteEndPoint, Me.Uri))
                                            If count <= 0 Then

                                                Debug.WriteLine("send done {0}", local.Client.RemoteEndPoint)
                                                local_done = True
                                                Return
                                            End If
                                            Await remote.SendAsync(New ArraySegment(Of Byte)(local_buffer, 0, count), WebSocketMessageType.Binary, False, CancellationToken.None)
                                            local_stream.BeginRead(local_buffer, 0, local_buffer.Length, local_callback, Nothing)

                                        Catch ex As Exception

                                            local_done = True
                                            'Throw

                                        End Try
                                    End Sub)
                            local_stream.BeginRead(local_buffer, 0, local_buffer.Length, local_callback, Nothing)

                            Try
                                Dim temp(1024) As Byte
                                Dim read_buffer As New ArraySegment(Of Byte)(temp)
                                Do While True

                                    Dim result = Await remote.ReceiveAsync(read_buffer, System.Threading.CancellationToken.None)
                                    'console_write(read_buffer.Array, result.Count, String.Format("{1} -> {0}", local.Client.RemoteEndPoint, Me.Uri))
                                    If result.Count <= 0 Then

                                        Debug.WriteLine("receive done {0}", local.Client.RemoteEndPoint)
                                        Exit Do
                                    End If
                                    local_stream.Write(read_buffer.Array, 0, result.Count)
                                Loop

                            Catch ex As Exception

                                'Throw

                            End Try

                            Do While Not local_done

                                System.Threading.Thread.Sleep(100L)
                            Loop
                            Debug.WriteLine("close {0}", local.Client.RemoteEndPoint)
                        End Using
                    End Using
                End Sub
            )
        listener.BeginAcceptTcpClient(accept, Nothing)

        Do While True

            System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite)
        Loop
    End Sub

    <Argument("u"c, , "uri")>
    Public Overridable Property Uri As String = ""

    <Argument("x"c, , "proxy-host")>
    Public Overridable Property ProxyHost As String = ""

    <Argument("y"c, , "proxy-port")>
    Public Overridable Property ProxyPort As Integer = 80

    <Argument("V"c, , "validation")>
    Public Overridable Property SSLValidationSkip As Boolean = False

    <Argument("H"c, , "local-host")>
    Public Overridable Property LocalHost As String = "localhost"

    <Argument("P"c, , "local-port")>
    Public Overridable Property LocalPort As Integer = 0

End Class
