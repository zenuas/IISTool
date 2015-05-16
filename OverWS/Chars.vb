Imports System
Imports System.Text


Public Class Chars

    Public Shared ReadOnly Null As Char = ToChar(0)  ' null
    Public Shared ReadOnly Bell As Char = ToChar(7)  ' bell
    Public Shared ReadOnly Del As Char = ToChar(8)   ' delete
    Public Shared ReadOnly Tab As Char = ToChar(9)   ' horizontal tab
    Public Shared ReadOnly Lf As Char = ToChar(10)   ' line feed
    Public Shared ReadOnly VTab As Char = ToChar(11) ' vertical tab
    Public Shared ReadOnly Ff As Char = ToChar(12)   ' form feed
    Public Shared ReadOnly Cr As Char = ToChar(13)   ' carriage return
    Public Shared ReadOnly Esc As Char = ToChar(27)  ' escape

    Public Shared Function ToInt(ByVal c As Char) As Integer

        Return Convert.ToInt32(c)
    End Function

    Public Shared Function ToChar(ByVal c As Integer) As Char

        Return ToChar(Convert.ToUInt32(c))
    End Function

    Public Shared Function ToChar(ByVal c As UInteger) As Char

        Return Convert.ToChar(c)
    End Function

    Public Shared Function Min(ByVal a As Char, ByVal b As Char) As Char

        If a < b Then Return a
        Return b
    End Function

    Public Shared Function Max(ByVal a As Char, ByVal b As Char) As Char

        If a < b Then Return b
        Return a
    End Function

    Public Shared Function Add(ByVal c As Char, ByVal inc As Integer) As Char

        Return ToChar(ToInt(c) + inc)
    End Function

End Class
