Imports System
Imports System.Collections.Generic
Imports System.Reflection


Public Class CommandLineParser

    Public Delegate Function EscapeCallback(ByVal s As String) As String

    Public Shared Function ArgumentsToString(ByVal ParamArray args() As String) As String

        Return ArgumentsToString(args, AddressOf ArgumentsMetaEscape)
    End Function

    Public Shared Function ArgumentsDoubleQuoteEscape(ByVal s As String) As String

        If s.Length = 0 OrElse s.IndexOfAny(New Char() {" "c, """"c}) >= 0 Then

            Return """" + s.Replace("""", """""") + """"
        End If

        Return s
    End Function

    Public Shared Function ArgumentsMetaEscape(ByVal s As String) As String

        If s.Length = 0 OrElse s.IndexOfAny(New Char() {" "c, "\"c, "'"c, """"c}) >= 0 Then

            Return """" + s.Replace("\", "\\").Replace("'", "\'").Replace("""", "\""") + """"
        End If

        Return s
    End Function

    Public Shared Function ArgumentsToString(ByVal args() As String, ByVal escape_callback As EscapeCallback) As String

        Dim s As New System.Text.StringBuilder

        For Each arg As String In args

            If s.Length > 0 Then s.Append(" ")
            s.Append(escape_callback(arg))
        Next

        Return s.ToString
    End Function

    Public Shared Function CommandParse(ByVal arg As String) As List(Of String)

        Dim args As New List(Of String)
        Dim command As New System.Text.StringBuilder

        Dim quote As Char = Chars.Null
        Dim isescape As Boolean = False
        Dim iscrlfescape As Boolean = False

        For i As Integer = 0 To arg.Length - 1

            Dim c As Char = arg.Chars(i)
            If c = Chars.Null Then Throw New ArgumentException("parse error: include null-char")

            If isescape OrElse iscrlfescape Then

                If c = Chars.Cr AndAlso Not iscrlfescape Then

                    isescape = False
                    iscrlfescape = True
                    Continue For
                End If

                If c = Chars.Lf Then

                    isescape = False
                    Continue For
                End If
            End If
            iscrlfescape = False

            If Not isescape Then

                If c = "\"c AndAlso quote <> "'"c Then

                    isescape = True
                    Continue For

                ElseIf quote = Chars.Null AndAlso (c = "'"c OrElse c = """"c) Then

                    quote = c
                    Continue For

                ElseIf quote <> Chars.Null AndAlso quote = c Then

                    quote = Chars.Null
                    Continue For
                End If
            End If

            Select Case c
                Case Chars.Cr, Chars.Lf, " "c, Chars.Tab

                    c = " "c
            End Select

            If quote <> Chars.Null Then

                command.Append(c)
            Else

                If c = " "c AndAlso Not isescape Then

                    If command.Length = 0 Then Continue For
                    args.Add(command.ToString)
                    command.Length = 0
                    Continue For
                End If
                command.Append(c)
            End If

            isescape = False
        Next
        If isescape OrElse quote <> Chars.Null Then Throw New ArgumentException("parse error: escape terminated")
        If command.Length > 0 Then args.Add(command.ToString)

        Return args
    End Function

#If UNDER_CE = 0 Then

    Public Overridable Function Parse() As String()

        Dim args As List(Of String) = CommandParse(System.Environment.CommandLine)
        args.RemoveAt(0)
        Return Me.Parse(args.ToArray)
    End Function

#End If

    Public Overridable Function Parse(ByVal arg As String) As String()

        Return Me.Parse(CommandParse(arg).ToArray)
    End Function

    Public Overridable Function Parse(ByVal args() As String) As String()

        Return Parse(Me, args)
    End Function

#If UNDER_CE = 0 Then

    Public Shared Function Parse(ByVal receiver As Object) As String()

        Dim args As List(Of String) = CommandParse(System.Environment.CommandLine)
        args.RemoveAt(0)
        Return Parse(receiver, args.ToArray)
    End Function

#End If

    Public Shared Function Parse(ByVal receiver As Object, ByVal arg As String) As String()

        Return Parse(receiver, CommandParse(arg).ToArray)
    End Function

    Public Shared Function Parse(ByVal receiver As Object, ByVal args() As String) As String()

        Dim run_methods As New List(Of System.Reflection.MethodInfo)
        Dim run_property As New List(Of System.Reflection.PropertyInfo)
        Dim opt_map As New Dictionary(Of String, System.Reflection.MethodInfo)

        For Each method As System.Reflection.MethodInfo In receiver.GetType.GetMethods(BindingFlags.FlattenHierarchy Or BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance)

            Dim opts() As Object = method.GetCustomAttributes(GetType(ArgumentAttribute), True)

            If opts.Length > 0 Then

                With CType(opts(0), ArgumentAttribute)

                    If .LongOption.Length > 0 Then

                        opt_map.Add(.LongOption, method)
                    Else
                        opt_map.Add(method.Name, method)
                    End If
                    If .ShortOption <> Chars.Null Then opt_map.Add(.ShortOption.ToString, method)
                End With
            End If
        Next

        For Each prop As System.Reflection.PropertyInfo In receiver.GetType.GetProperties(BindingFlags.FlattenHierarchy Or BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance Or BindingFlags.SetProperty)

            Dim opts() As Object = prop.GetCustomAttributes(GetType(ArgumentAttribute), True)
            If opts.Length > 0 Then

                With CType(opts(0), ArgumentAttribute)

                    If .LongOption.Length > 0 Then

                        opt_map.Add(.LongOption, prop.GetSetMethod(True))
                    Else
                        opt_map.Add(prop.Name, prop.GetSetMethod(True))
                    End If
                    If .ShortOption <> Chars.Null Then opt_map.Add(.ShortOption.ToString, prop.GetSetMethod(True))
                End With
            End If
        Next


        Dim cur_method As System.Reflection.MethodInfo = Nothing
        Dim cur_prop As System.Reflection.PropertyInfo = Nothing
        Dim arg_collect As New List(Of Object)
        Dim run_collect As New List(Of String)

        For i As Integer = 0 To args.Length - 1

            If args(i).Length = 0 Then Continue For
            If args(i).Length > 1 AndAlso args(i).Chars(0) = "-"c Then

                Dim optname As String
                Dim argument As String = Nothing

                If args(i).Chars(1) = "-"c Then

                    optname = args(i).Substring(2)
                    If optname.IndexOf("="c) >= 0 Then

                        argument = optname.Substring(optname.IndexOf("="c) + 1)
                        optname = optname.Substring(0, optname.IndexOf("="c))
                    End If
                Else

                    If args(i).Length > 2 Then argument = args(i).Substring(2)
                    optname = args(i).Chars(1).ToString
                End If
                cur_method = opt_map(optname)

                If argument IsNot Nothing Then

                    arg_collect.Add(ToArgType(argument, cur_method.GetParameters(arg_collect.Count).ParameterType))
                    GoTo ExecuteOption
                End If

            ElseIf cur_method IsNot Nothing Then

                arg_collect.Add(ToArgType(args(i), cur_method.GetParameters(arg_collect.Count).ParameterType))
            Else

                run_collect.Add(args(i))
            End If

            If cur_method IsNot Nothing AndAlso cur_method.GetParameters.Length = arg_collect.Count Then

ExecuteOption:
                cur_method.Invoke(receiver, arg_collect.ToArray)
                arg_collect.Clear()
                cur_method = Nothing
            End If
        Next

        Return run_collect.ToArray
    End Function

    Private Shared Function ToArgType(ByVal o As Object, ByVal [type] As System.Type) As Object

        If _
            type Is GetType(System.IO.TextReader) OrElse _
            type Is GetType(System.IO.StreamReader) Then

            If o.ToString.Equals("-") Then

                Return System.Console.In
            Else
                Return New System.IO.StreamReader(New System.IO.FileStream(o.ToString, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read))
            End If

        ElseIf _
            type Is GetType(System.IO.TextWriter) OrElse _
            type Is GetType(System.IO.StreamWriter) Then

            If o.ToString.Equals("-") Then

                Return System.Console.Out
            Else
                Dim out As New System.IO.StreamWriter(New System.IO.FileStream(o.ToString, IO.FileMode.Create, IO.FileAccess.Write, IO.FileShare.Write))
                out.AutoFlush = True
                Return out
            End If

        ElseIf type.GetMethod("Parse", New System.Type() {o.GetType}) IsNot Nothing Then

            Return type.InvokeMember("Parse", BindingFlags.InvokeMethod, Nothing, Nothing, New Object() {o})
        Else
            Return o.ToString
        End If
    End Function

    <Argument("h"c, , "help")> _
    Protected Overridable Sub Help()

        Console.WriteLine(HelpMessage(Me))
#If UNDER_CE = 0 Then
        System.Environment.Exit(0)
#End If
    End Sub

#If UNDER_CE = 0 Then

    <Argument("V"c, , "version")> _
    Protected Overridable Sub Version()

        Console.Write(System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetEntryAssembly().Location))
        System.Environment.Exit(0)
    End Sub

#End If

    Public Shared Function HelpMessage(ByVal receiver As Object) As String

        Dim message As New System.Text.StringBuilder

        For Each method As System.Reflection.MethodInfo In receiver.GetType.GetMethods(BindingFlags.FlattenHierarchy Or BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance)

            Dim opts() As Object = method.GetCustomAttributes(GetType(ArgumentAttribute), True)

            If opts.Length > 0 Then

                With CType(opts(0), ArgumentAttribute)

                    message.Append(Chars.Tab)
                    If .ShortOption <> Chars.Null Then message.Append("-" + .ShortOption.ToString + " ")
                    If .LongOption.Length > 0 Then

                        message.Append("--" + .LongOption + " ")
                    Else
                        message.Append("--" + method.Name + " ")
                    End If

                    'message.AppendLine("") ' .NET Compact Framework not supported
                    message.Append(Chars.Lf)
                End With
            End If
        Next

        Return message.ToString
    End Function

End Class
