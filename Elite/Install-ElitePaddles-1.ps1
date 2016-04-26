param
(
    [String]
    $SlnPath = "Elite.sln"
)

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal -ArgumentList $identity
if(-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator))
{
    throw "Please run this script with administrative privileges."
}

Write-Warning @"
Running this script will perform the following actions on your PC:
Modify the dependencies of the project files to point to the directory of your XboxDevices app.
"@

Write-Host "Press any key to continue ..."
$x = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

if (Get-AppxPackage -Name "ElitePaddles")
{
    throw "ElitePaddles app already installed. Please remove it if you wish to proceed."
}

$systemType = (gwmi win32_computersystem).SystemType
if($systemType.StartsWith("x64"))
{
	$procArch = "x64"
}
else
{
	$procArch = "x86"
}

if(-not ($package = Get-AppxPackage -Name Microsoft.XboxDevices))
{
    throw "Failed to find the Microsoft.XboxDevices appx package. Please install the Xbox Accessories app from the Windows Store."
}
$xboxDevicesLocation = $package.InstallLocation

# Verify that Elite.sln exists and find its directory from the provided parameters
if(-not ($SlnPath.EndsWith("Elite.sln") -and (Test-Path $SlnPath -PathType Leaf)))
{
    throw "Parameter SlnPath does not resolve to 'Elite.sln'.";
}
$slnDir = $SlnPath | Resolve-Path | Split-Path

# Verify that build dependency exists
$msbuildLocation = gci "C:\Program Files*\MSBuild\14.0\Bin\MSBuild.exe" | Select -First 1 | Resolve-Path | Convert-Path
if(-not (Test-Path $msbuildLocation -PathType Leaf))
{
    throw "Could not find MSBuild.exe in . Please make sure you have the Microsoft Build Tools installed (https://www.microsoft.com/en-us/download/details.aspx?id=48159)."
}

# Verify that installutil exists to install the service
$installUtilLocation = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\installutil.exe"
if(-not (Test-Path $installUtilLocation -PathType Leaf))
{
    throw "Could not find Installutil.exe in C:\Windows\Microsoft.NET\Framework\v4.0.30319\. Please make sure the necessary version of the .NET Framework is installed."
}

#Verify that the certificate creation and signing tools exists
$makeCertLocation = gci "C:\Program Files*\Windows Kits\10\bin\x86\makecert.exe" | Select -First 1 | Resolve-Path | Convert-Path
$pvk2pfxLocation = gci "C:\Program Files*\Windows Kits\10\bin\x86\pvk2pfx.exe" | Select -First 1 | Resolve-Path | Convert-Path
$signtoolLocation = gci "C:\Program Files*\Windows Kits\10\bin\x86\signtool.exe" | Select -First 1 | Resolve-Path | Convert-Path
if(-not ((Test-Path $makeCertLocation -PathType Leaf) -and (Test-Path $pvk2pfxLocation -PathType Leaf) -and (Test-Path $signtoolLocation)))
{
    throw "Could not find makecert.exe, pvk2pfx.exe, or signtool.exe in C:\Program Files*\Windows Kits\10\bin\x86\. Please make sure you have the Windows 10 SDK installed (https://dev.windows.com/en-us/downloads/windows-10-sdk)."
}

# Assume the csproj files exists and change their dependencies to point to the Xbox Accessories app directory
$eliteUiCsprojLocation = $slnDir + "\EliteUi\EliteUi.csproj"
if ($xboxDevicesLocation)
{
    $xml = [xml](Get-Content -Path $eliteUiCsprojLocation)
    $xml.Project.ItemGroup.Reference | ? { $_.HintPath -and $_.HintPath.Contains("XboxDevices") } | % { $file = $_.HintPath | Split-Path -Leaf; $_.HintPath = $xboxDevicesLocation + "\" + $file }
    $xml.Save($eliteUiCsprojLocation)
}

Write "Now build the solution in Visual Studio according to the instructions."