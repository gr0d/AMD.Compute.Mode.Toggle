Public Class cGPUInfo

    Public AdapterInstace As Integer = 0
    Public AdapterDescription As String
    Public AdapterDriverVersion As String = String.Empty
    Public DeviceID As String = String.Empty
    Public IsAMD As Boolean
    Public ComputeMode As Boolean = False
    Public IgnoreAdapter As Boolean = False
    Public RegKey As Microsoft.Win32.RegistryKey


End Class
