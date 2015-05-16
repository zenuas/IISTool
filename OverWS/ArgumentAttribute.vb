Imports System


Public Class ArgumentAttribute
    Inherits Attribute

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal help As String, Optional ByVal longopt As String = "", Optional ByVal isobsolute As Boolean = False)
        Me.New()

        Me._help = help
        Me._longopt = longopt
        Me._isobsolute = isobsolute
    End Sub

    Public Sub New(ByVal shortopt As Char, Optional ByVal help As String = "", Optional ByVal longopt As String = "", Optional ByVal isobsolute As Boolean = False)
        Me.New()

        Me._shortopt = shortopt
        Me._help = help
        Me._longopt = longopt
        Me._isobsolute = isobsolute
    End Sub

    Private _shortopt As Char = Chars.Null
    Public Overridable ReadOnly Property ShortOption() As Char
        Get
            Return Me._shortopt
        End Get
    End Property

    Private _help As String = ""
    Public Overridable ReadOnly Property Help() As String
        Get
            Return Me._help
        End Get
    End Property

    Private _longopt As String = ""
    Public Overridable ReadOnly Property LongOption() As String
        Get
            Return Me._longopt
        End Get
    End Property

    Private _isobsolute As Boolean = False
    Public Overridable ReadOnly Property IsObsolute() As Boolean
        Get
            Return Me._isobsolute
        End Get
    End Property

End Class
