
Imports System.Text.RegularExpressions
Module Module1

    Private GPUList As New List(Of cGPUInfo)
    Private AdapterInstance As Integer


    Sub Main()

        Dim HT As Hashtable = GetCommandLineArgs()
        Dim theMode As eMode = ValidateCommandlinearguments(HT)
        GetAdapters()

        Console.WriteLine()
        Console.WriteLine(theMode.ToString)

        Select Case theMode
            Case eMode.DisableAll

                ModifyComputeModeAll(0)

                Console.WriteLine()
                Console.ForegroundColor = ConsoleColor.Red
                Console.WriteLine("Please reboot your device for changes to take effect!")
                Console.ForegroundColor = ConsoleColor.White

            Case eMode.EnableAll

                ModifyComputeModeAll(2)

                Console.WriteLine()
                Console.ForegroundColor = ConsoleColor.Red
                Console.WriteLine("Please reboot your device for changes to take effect!")
                Console.ForegroundColor = ConsoleColor.White

            Case eMode.EnableSpecific

                For Each item In HT.Item("/a").ToString.Split(",")
                    ModifySpecific(2, item)
                Next

                Console.WriteLine()
                Console.ForegroundColor = ConsoleColor.Red
                Console.WriteLine("Please reboot your device for changes to take effect!")
                Console.ForegroundColor = ConsoleColor.White

            Case eMode.DisableSpecific

                For Each item In HT.Item("/a").ToString.Split(",")
                    ModifySpecific(0, item)
                Next

                Console.WriteLine()
                Console.ForegroundColor = ConsoleColor.Red
                Console.WriteLine("Please reboot your device for changes to take effect!")
                Console.ForegroundColor = ConsoleColor.White

            Case eMode.ListOnly

                DisplayAdapterInfo()

            Case eMode.ShowSyntax

                DisplaySyntax()

        End Select

    End Sub


    Private Sub DisplayAdapterInfo()

        Console.WriteLine("")
        Console.ForegroundColor = ConsoleColor.Yellow
        Console.WriteLine("{0}", My.Application.Info.AssemblyName & " " & My.Application.Info.Version.ToString)

        Console.ForegroundColor = ConsoleColor.White

        Console.WriteLine("")

        For Each GPUInfo As cGPUInfo In GPUList

            If GPUInfo.IsAMD = True Then

                Console.WriteLine(String.Format("Adapter Description:    {0}", GPUInfo.AdapterDescription))
                Console.WriteLine(String.Format("Adapter Instance:       {0}", GPUInfo.AdapterInstace))
                Console.WriteLine(String.Format("Driver Version:         {0}", GPUInfo.AdapterDriverVersion))
                Console.WriteLine(String.Format("Device ID:              {0}", GPUInfo.DeviceID))
                Console.WriteLine(String.Format("IsAMD:                  {0}", GPUInfo.IsAMD))
                Console.WriteLine(String.Format("Compute Mode Enabled:   {0}", GPUInfo.ComputeMode))
                Console.WriteLine("")

            End If

        Next

        Console.WriteLine(String.Format("Total Adapter(s) Found: {0}", GPUList.Count))
        Console.WriteLine(String.Format("AMD Adapter(s) Found:   {0}", GPUList.FindAll(Function(x) x.IsAMD = True).Count))

    End Sub



    Private Sub GetAdapters()

        Dim RK As Microsoft.Win32.RegistryKey =
            Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine,
                                                    Microsoft.Win32.RegistryView.Registry64).OpenSubKey(My.Settings.DisplayDriverRootPath)

        Dim GPUInfo As cGPUInfo = Nothing

        Try

            For Each item In RK.GetSubKeyNames

                GPUInfo = New cGPUInfo

                Dim SK As Microsoft.Win32.RegistryKey

                If RK.OpenSubKey(item).GetValueNames.Contains(My.Settings.DisplayDriverDescriptionValueName) Then

                    SK = RK.OpenSubKey(item, True)

                    GPUInfo.RegKey = SK
                    GPUInfo.AdapterDescription = SK.GetValue(My.Settings.DisplayDriverDescriptionValueName)
                    GPUInfo.AdapterDriverVersion = SK.GetValue("DriverVersion")
                    GPUInfo.DeviceID = SK.GetValue("MatchingDeviceId")

                    For Each Inclusion As String In My.Settings.DisplayIncludeList.Split("|")

                        If GPUInfo.AdapterDescription.ToLower.Contains(Inclusion.ToLower) Then
                            AdapterInstance = AdapterInstance + 1
                            GPUInfo.IsAMD = True
                            GPUInfo.AdapterInstace = AdapterInstance
                            Exit For
                        End If

                    Next

                    GPUInfo.ComputeMode = SK.GetValue("KMD_EnableInternalLargePage")

                    If GPUInfo.ComputeMode = 2 Then GPUInfo.ComputeMode = True

                End If

                GPUList.Add(GPUInfo)
            Next

        Catch ex As Exception
            Console.WriteLine(ex.Message)
        End Try

    End Sub


    Private Sub ModifyComputeModeAll(ByVal Mode)

        Console.WriteLine()
        Console.WriteLine("Modifying all adapaters")
        Console.WriteLine()
        For Each GPUInfo As cGPUInfo In GPUList

            If GPUInfo.IsAMD = True Then

                Try

                    Dim RK As Microsoft.Win32.RegistryKey = GPUInfo.RegKey
                    RK.SetValue(My.Settings.ComputeModeValueName, Mode, Microsoft.Win32.RegistryValueKind.DWord)
                    Console.WriteLine(String.Format("Adapter: {0} (Success)", GPUInfo.AdapterInstace))

                Catch ex As Exception

                    Console.WriteLine(String.Format("Adapter: {0} (Failed {1})", GPUInfo.AdapterInstace, ex.Message))

                End Try

            End If

        Next

    End Sub

    Private Sub ModifySpecific(ByVal Mode As Object, ByVal Instance As Object)

        Dim GPUInfo = GPUList.Find(Function(x) x.AdapterInstace = Instance)

        If GPUInfo IsNot Nothing Then

            Try
                Console.WriteLine()
                Dim RK As Microsoft.Win32.RegistryKey = GPUInfo.RegKey
                RK.SetValue(My.Settings.ComputeModeValueName, Mode, Microsoft.Win32.RegistryValueKind.DWord)
                Console.WriteLine(String.Format("Adapter: {0} (Success)", GPUInfo.AdapterInstace))

            Catch ex As Exception

                Console.WriteLine(String.Format("Adapter: {0} (Failed {1})", GPUInfo.AdapterInstace, ex.Message))

            End Try
        Else

            Console.WriteLine(String.Format("Adapter Instance {0} not found! ", Instance))
            Console.WriteLine(String.Format("Use /L to see a list of available adapters."))
        End If




    End Sub


    Private Function GetCommandLineArgs() As Hashtable

        Dim Args As String() = Environment.GetCommandLineArgs
        Dim Regex As Regex = New Regex("(/[LR])|(/M:[01])|/A:(\d+)(,\s*\d+)*", RegexOptions.IgnoreCase)
        Dim AL As New Hashtable

        For Each arg As String In Args

            Dim Match As Match = Regex.Match(arg)

            If Match.Success Then

                If Match.Value.ToLower.Contains("/m") Then
                    AL.Add(Match.ToString.ToLower.Split(":")(0), Match.ToString.ToLower.Split(":")(1))
                ElseIf Match.Value.tolower.Contains("/a") Then
                    AL.Add(Match.ToString.ToLower.Split(":")(0), Match.ToString.ToLower.Split(":")(1))
                Else
                    AL.Add(Match.ToString.ToLower, "")
                End If

            End If

        Next

        Return AL

    End Function

    Private Function ValidateCommandlinearguments(ByVal aAL As Hashtable) As eMode


        If aAL.ContainsKey("/l") And aAL.Count = 1 Then

            Return eMode.ListOnly


        ElseIf aAL.ContainsKey("/m") And aAL.Item("/m") = 1 And aAL.ContainsKey("/a") Then

            Return eMode.EnableSpecific

        ElseIf aAL.ContainsKey("/m") And aAL.Item("/m") = 0 And aAL.ContainsKey("/a") Then

            Return eMode.DisableSpecific

        ElseIf aAL.ContainsKey("/m") And aAL.Item("/m") = 1 Then

            Return eMode.EnableAll

        ElseIf aAL.ContainsKey("/m") And aAL.Item("/m") = 0 Then

            Return eMode.DisableAll
        End If


        Return eMode.ShowSyntax

    End Function

    Private Sub DisplaySyntax(Optional ByVal InvalidSyntax As Boolean = False, Optional ByVal aMessage As String = "")

        If InvalidSyntax Then

            Console.ForegroundColor = ConsoleColor.Red

            Console.WriteLine()
            Console.WriteLine(String.Format("Invalid Syntax: {0}", aMessage))
            Console.WriteLine()

        End If

        Console.WriteLine()
        Console.ForegroundColor = ConsoleColor.Yellow
        Console.WriteLine(String.Format("{0} {1}", My.Application.Info.AssemblyName, "[/L] [/M:0/1] [/A:1,2,4,5] [/R]"))
        Console.ForegroundColor = ConsoleColor.White

        Console.WriteLine()
        Console.WriteLine("/L      List all AMD Devices and its configuration")
        Console.WriteLine("/M      Enable or Disable Compute Mode.  0 Disable, 1 Enable")
        Console.WriteLine("/A      Specify devices to modify.  Devices mnust be separated by commas.")
        Console.WriteLine("/R      Reboot device after change is  made, default is not to reboot.")
        Console.WriteLine()

        Console.WriteLine("Example: {0} {1} ", My.Application.Info.AssemblyName, "/L Lists all AMD devcies and their current compute mode setting.  /L Can only be used by itself.")
        Console.WriteLine("Example: {0} {1} ", My.Application.Info.AssemblyName, "/M:0 - Disable compute mode for all AMD devices.  /M Can only be used with /A /R")
        Console.WriteLine("Example: {0} {1} ", My.Application.Info.AssemblyName, "/M:1 - Enable compute mode for all AMD devices.  /M Can only be used with /A /R")
        Console.WriteLine("Example: {0} {1} ", My.Application.Info.AssemblyName, "/M:0 /R - Disable compute mode for all AMD devices and reboot.  /M Can only be used with /A /R")
        Console.WriteLine("Example: {0} {1} ", My.Application.Info.AssemblyName, "/M:1 /R - Enable compute mode for all AMD devices. and reboot. /M Can only be used with /A /R")
        Console.WriteLine("Example: {0} {1} ", My.Application.Info.AssemblyName, "/M:1 /A:1,3,6,7 /R - Enable compute mode for AMD devices number 1, 3, 6, 7 and reboot.  /M Can only be used with /A /R")
        Console.WriteLine("Example: {0} {1} ", My.Application.Info.AssemblyName, "/M:0 /A:1,3,6,7 /R - Disable compute mode for AMD devices number 1, 3, 6, 7 and reboot.  /M Can only be used with /A /R")


        Environment.Exit(0)


    End Sub


    Private Enum eMode
        ListOnly
        DisableAll
        EnableAll
        DisableSpecific
        EnableSpecific
        ShowSyntax
    End Enum




End Module
