<#
.SYNOPSIS
    Builds the service, the UI and the installer of Printhead Maintainer.

.DESCRIPTION
    First checks that everything the build needs is installed and lists whatever is missing. Then
    builds the service, the UI and the installer, and copies the installer to the artifacts folder.

.PARAMETER Configuration
    Release (the default) or Debug.

.PARAMETER PlatformToolset
    The C++ toolset that builds the service, by default the one the service project uses.

.PARAMETER AcceptWixEula
    Accepts the Open Source Maintenance Fee EULA of WiX Toolset for this build. WiX, which builds the
    installer, refuses to run unless its EULA is accepted. The fee only applies to revenue-generating
    use by those with an annual gross revenue of US$10,000 or more.

.EXAMPLE
    .\build.ps1 -AcceptWixEula

.EXAMPLE
    .\build.ps1 -AcceptWixEula -Configuration Debug
#>
[CmdletBinding()]
param(
    [ValidateSet('Release', 'Debug')]
    [string] $Configuration = 'Release',

    [string] $PlatformToolset,

    [switch] $AcceptWixEula
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    Write-Host 'Printhead Maintainer can only be built on Windows.' -ForegroundColor Red
    exit 1
}

$root = $PSScriptRoot
$serviceProject = Join-Path $root 'PrintheadMaintainerService\PrintheadMaintainer\PrintheadMaintainer.vcxproj'
$installerProject = Join-Path $root 'PrintheadMaintainerInstaller\PrintheadMaintainerInstaller.wixproj'
$installer = Join-Path $root "PrintheadMaintainerInstaller\bin\x64\$Configuration\PrintheadMaintainerInstaller.msi"
$artifacts = Join-Path $root 'artifacts'
$version = ([xml](Get-Content (Join-Path $root 'Directory.Build.props') -Raw)).Project.PropertyGroup.Version

if (-not $PlatformToolset) {
    $namespace = @{ msb = 'http://schemas.microsoft.com/developer/msbuild/2003' }
    $PlatformToolset = (Select-Xml -Path $serviceProject -XPath '//msb:PlatformToolset' -Namespace $namespace |
        Select-Object -First 1).Node.InnerText
}

# The EULA of WiX Toolset vN has the id wixN; the version is the one the installer project uses.
$wixMajorVersion = [regex]::Match((Get-Content $installerProject -Raw), 'WixToolset\.Sdk/(\d+)\.').Groups[1].Value
$wixEulaId = "wix$wixMajorVersion"

Write-Host "Checking the build environment for Printhead Maintainer $version ($Configuration)"
$problems = New-Object System.Collections.Generic.List[string]

# Visual Studio 2019 or later, or its Build Tools, with the C++ toolset. The newest installation
# that has the toolset builds everything.
$msbuild = $null
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$visualStudios = @()
if (Test-Path $vswhere) {
    # The installation folders, newest version first.
    $visualStudios = @(& $vswhere -products '*' -requires Microsoft.Component.MSBuild -sort -property installationPath)
}
if ($visualStudios.Count -eq 0) {
    $problems.Add('Visual Studio is not installed. Install Visual Studio 2026, or the Build Tools for Visual Studio 2026, with the "Desktop development with C++" and ".NET desktop development" workloads (".NET desktop build tools" for the Build Tools).')
}
else {
    $toolsetFolders = 'MSBuild\Microsoft\VC\*\Platforms\x64\PlatformToolsets'
    $withToolset = $visualStudios |
        Where-Object { Test-Path (Join-Path $_ "$toolsetFolders\$PlatformToolset") } |
        Select-Object -First 1
    if ($withToolset) {
        $msbuild = Join-Path $withToolset 'MSBuild\Current\Bin\MSBuild.exe'
        # The UI also needs the C# compiler and NuGet, which the C++ workload alone does not install.
        if (-not (Test-Path (Join-Path $withToolset 'MSBuild\Current\Bin\Roslyn\csc.exe')) -or
            -not (Test-Path (Join-Path $withToolset 'Common7\IDE\CommonExtensions\Microsoft\NuGet\NuGet.targets'))) {
            $problems.Add("The .NET desktop build tools are not installed in $withToolset. Add the "".NET desktop development"" workload ("".NET desktop build tools"" for the Build Tools) with the Visual Studio Installer.")
        }
    }
    else {
        $installedToolsets = @($visualStudios |
            ForEach-Object { Get-ChildItem (Join-Path $_ "$toolsetFolders\*") -Directory -ErrorAction SilentlyContinue } |
            Where-Object { $_.Name -match '^v\d+$' } | Select-Object -ExpandProperty Name | Sort-Object -Unique -Descending)
        $problem = "The C++ toolset $PlatformToolset that builds the service is not installed. Install the Visual Studio version that provides it, with its C++ workload"
        if ($installedToolsets.Count -gt 0) {
            $problem += ", or build with an installed one: -PlatformToolset $($installedToolsets[0])"
        }
        $problems.Add($problem + '.')
    }
}

# The Windows SDK the service is built against; the project takes the newest one installed.
$windowsSdkFound = $false
foreach ($sdkKey in 'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Microsoft SDKs\Windows\v10.0', 'HKLM:\SOFTWARE\Microsoft\Microsoft SDKs\Windows\v10.0') {
    $sdkFolder = try { Get-ItemPropertyValue $sdkKey -Name InstallationFolder } catch { $null }
    if ($sdkFolder -and (Test-Path (Join-Path $sdkFolder 'Include\*\um\Windows.h'))) {
        $windowsSdkFound = $true
        break
    }
}
if (-not $windowsSdkFound) {
    $problems.Add('The Windows 10 or 11 SDK is not installed. Add it with the Visual Studio Installer.')
}

# The UI targets .NET Framework 4.7.2.
$referenceAssemblies = Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2'
if (-not (Test-Path (Join-Path $referenceAssemblies 'mscorlib.dll'))) {
    $problems.Add('The .NET Framework 4.7.2 targeting pack is not installed. Add it with the Visual Studio Installer.')
}

# WiX keeps a file in the user profile once its EULA is accepted for the account (see the README).
$wixEulaFile = Join-Path $env:USERPROFILE ".wix\$wixEulaId-osmf-eula.txt"
if (-not $AcceptWixEula -and -not (Test-Path $wixEulaFile)) {
    $problems.Add("WiX Toolset v$wixMajorVersion, which builds the installer, needs its Open Source Maintenance Fee EULA accepted: https://github.com/wixtoolset/wix/blob/main/OSMFEULA.txt. The fee only applies to revenue-generating use by those with an annual gross revenue of US`$10,000 or more. To accept it, build with -AcceptWixEula.")
}

if ($problems.Count -gt 0) {
    Write-Host 'The build cannot start:' -ForegroundColor Red
    foreach ($problem in $problems) {
        Write-Host "- $problem" -ForegroundColor Red
    }
    exit 1
}
Write-Host "Using $msbuild with the C++ toolset $PlatformToolset"

function Invoke-MSBuild([string] $Title, [string[]] $Arguments) {
    Write-Host "==> $Title" -ForegroundColor Cyan
    & $msbuild -nologo -m -v:minimal "-p:Configuration=$Configuration" @Arguments
    if ($LASTEXITCODE -ne 0) {
        Write-Host "$Title failed." -ForegroundColor Red
        exit 1
    }
}

Invoke-MSBuild 'Building the service' @(
    (Join-Path $root 'PrintheadMaintainerService\PrintheadMaintainer.sln'), '-p:Platform=x64', "-p:PlatformToolset=$PlatformToolset")
Invoke-MSBuild 'Building the UI' @(
    (Join-Path $root 'PrintheadMaintainerUI\PrintheadMaintainerUI.sln'), '-restore')
$installerArguments = @((Join-Path $root 'PrintheadMaintainerInstaller\PrintheadMaintainerInstaller.sln'), '-restore', '-p:Platform=x64')
if ($AcceptWixEula) {
    $installerArguments += "-p:AcceptEula=$wixEulaId"
}
Invoke-MSBuild 'Building the installer' $installerArguments

if (Test-Path $artifacts) {
    Remove-Item $artifacts -Recurse -Force
}
New-Item $artifacts -ItemType Directory | Out-Null
Copy-Item $installer $artifacts
Write-Host "Built $(Join-Path $artifacts (Split-Path $installer -Leaf))" -ForegroundColor Green
