<#
.SYNOPSIS
    Tests the installer of Printhead Maintainer by installing, repairing and uninstalling the program.

.DESCRIPTION
    Installs the program and checks the installation: the files, the service, which writes its log,
    the settings and the start of the UI at sign-in. Then repairs the program and uninstalls it, each
    time while the UI runs, and checks that the setup closed the UI, which has to end by itself, and
    needs no restart. After the uninstall, the installation folder with the data folder, the
    service, the settings, the start of the UI at sign-in and the Start menu folder have to be gone.

    The program is really installed and uninstalled, so run the test as administrator on a computer
    where Printhead Maintainer is not installed, such as the one of the build workflow, after
    build.ps1. The logs of the setup and the log of the service are kept in the log folder.

.PARAMETER Installer
    The installer to test, by default the one build.ps1 puts in the artifacts folder.

.PARAMETER LogFolder
    The folder for the logs, by default the installer-test folder.

.EXAMPLE
    .\test-installer.ps1
#>
[CmdletBinding()]
param(
    [string] $Installer = (Join-Path $PSScriptRoot 'artifacts\PrintheadMaintainerInstaller.msi'),

    [string] $LogFolder = (Join-Path $PSScriptRoot 'installer-test')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    Write-Host 'The installer can only be tested on Windows.' -ForegroundColor Red
    exit 1
}

$serviceName = 'PrintheadMaintenanceSvc'
$installFolder = Join-Path $env:ProgramFiles 'Printhead Maintainer'
$serviceLog = Join-Path $installFolder 'Data\Logs\PrintheadMaintainer.log'
$customImage = Join-Path $installFolder 'Data\PrintImage.bmp'
$settingsKey = 'HKLM:\SOFTWARE\evilhawk00\Printhead Maintainer'
$runKey = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run'
$runValue = 'Printhead Maintainer'
$startMenuFolder = Join-Path $env:ProgramData 'Microsoft\Windows\Start Menu\Programs\Printhead Maintainer'

$principal = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host 'The installer test needs administrator rights.' -ForegroundColor Red
    exit 1
}
if (-not (Test-Path $Installer)) {
    Write-Host "The installer $Installer is not found. Build it with build.ps1 first." -ForegroundColor Red
    exit 1
}
$Installer = (Resolve-Path $Installer).Path
if ((Get-Service $serviceName -ErrorAction SilentlyContinue) -or (Test-Path $installFolder)) {
    Write-Host 'Printhead Maintainer is installed on this computer. The test needs a computer without it.' -ForegroundColor Red
    exit 1
}
New-Item $LogFolder -ItemType Directory -Force | Out-Null

$failed = $false

function Assert-That([bool] $Condition, [string] $Expectation, [string] $Detail) {
    if ($Condition) {
        Write-Host "  ok: $Expectation"
    }
    else {
        Write-Host "  FAILED: $Expectation$(if ($Detail) { " ($Detail)" })" -ForegroundColor Red
        $script:failed = $true
    }
}

# Checks the condition every half second until it is met or the time is up.
function Wait-Until([scriptblock] $Condition, [int] $Seconds) {
    $deadline = (Get-Date).AddSeconds($Seconds)
    while (-not (& $Condition)) {
        if ((Get-Date) -ge $deadline) {
            return $false
        }
        Start-Sleep -Milliseconds 500
    }
    return $true
}

# Starts the UI as at sign-in, and returns its process once it runs.
function Start-UI {
    Write-Host '==> Starting the UI' -ForegroundColor Cyan
    $ui = Start-Process (Join-Path $installFolder 'PrintheadMaintainer.exe') -ArgumentList '/silent' -PassThru
    # Taking the handle now keeps the exit code available after the UI has ended.
    $null = $ui.Handle
    $started = try { $ui.WaitForInputIdle(60000) } catch { $false } # throws if the UI has ended
    Start-Sleep -Seconds 3
    $running = $started -and -not $ui.HasExited
    Assert-That $running 'the UI runs'
    if ($running) {
        return $ui
    }
}

# Checks that the setup closed the UI. The setup ends a UI that does not close when asked with
# taskkill, which leaves exit code 1.
function Assert-UIClosed([System.Diagnostics.Process] $Process) {
    if ($Process) {
        $ended = $Process.WaitForExit(10000)
        Assert-That $ended 'the UI has ended'
        if ($ended) {
            Assert-That ($Process.ExitCode -eq 0) 'the UI closed by itself when asked' "exit code $($Process.ExitCode)"
        }
    }
}

# Runs msiexec without its window and with a verbose log, and returns whether it succeeded.
function Invoke-Setup([string] $Title, [string] $LogName, [string[]] $Arguments) {
    Write-Host "==> $Title" -ForegroundColor Cyan
    $log = Join-Path $LogFolder "$LogName.log"
    $setup = Start-Process msiexec.exe -ArgumentList ($Arguments + @('/qn', '/norestart', '/l*v', """$log""")) -Wait -PassThru
    # 3010 means that the setup succeeded but needs a restart, for example as a file was in use.
    Assert-That ($setup.ExitCode -eq 0) 'the setup succeeds without needing a restart' "exit code $($setup.ExitCode), see $log"
    return $setup.ExitCode -eq 0
}

if (-not (Invoke-Setup 'Installing' 'install' @('/i', """$Installer"""))) {
    Write-Host 'The installer test failed.' -ForegroundColor Red
    exit 1
}
foreach ($file in 'PrintheadMaintainSvc.exe', 'PrintheadMaintainer.exe', 'Resources\DefaultPrint.bmp') {
    Assert-That (Test-Path (Join-Path $installFolder $file)) "$file is installed"
}
$service = Get-Service $serviceName -ErrorAction SilentlyContinue
Assert-That ($service -and $service.Status -eq 'Running') 'the service runs'
# The service writes its log with its own account, which may only change the data folder.
$serviceStarted = Wait-Until { (Test-Path $serviceLog) -and (Select-String -Path $serviceLog -Pattern 'Service started' -Quiet) } 30
Assert-That $serviceStarted 'the service writes its log in the data folder'
Assert-That (Test-Path $settingsKey) 'the settings key is created'
Assert-That ($null -ne (Get-ItemProperty $runKey -Name $runValue -ErrorAction SilentlyContinue)) 'the UI starts at sign-in'
Assert-That (Test-Path $startMenuFolder) 'the Start menu folder is created'
if (Test-Path $serviceLog) {
    Copy-Item $serviceLog $LogFolder
}

# A custom image, which the user chooses and the setup does not install, has to be removed too.
Set-Content -Path $customImage -Value 'custom image'

# Windows closes programs whose files are in use in a quiet setup unless its Restart Manager is
# turned off. Turned off, a repair or an uninstall only finishes without a restart if the setup
# closes the UI.
$noRestartManager = 'MSIRESTARTMANAGERCONTROL=Disable'

$ui = Start-UI
# As the Repair button of the setup does, the repair reinstalls the files of the same version too.
$repaired = Invoke-Setup 'Repairing while the UI runs' 'repair' @('/i', """$Installer""", 'REINSTALL=ALL', 'REINSTALLMODE=ecmus', $noRestartManager)
Assert-UIClosed $ui
if ($repaired) {
    $service = Get-Service $serviceName -ErrorAction SilentlyContinue
    Assert-That ($service -and $service.Status -eq 'Running') 'the service runs again'
    Assert-That (Test-Path $customImage) 'the custom image is kept'
    Assert-That (Test-Path $settingsKey) 'the settings key is kept'
}

$ui = Start-UI
$uninstalled = Invoke-Setup 'Uninstalling while the UI runs' 'uninstall' @('/x', """$Installer""", $noRestartManager)
Assert-UIClosed $ui
if ($uninstalled) {
    $left = @(Get-ChildItem $installFolder -Recurse -Force -ErrorAction SilentlyContinue | ForEach-Object FullName)
    Assert-That (-not (Test-Path $installFolder)) 'the installation folder is removed, with the data folder' "left: $($left -join ', ')"
    Assert-That (-not (Get-Service $serviceName -ErrorAction SilentlyContinue)) 'the service is removed'
    Assert-That (-not (Test-Path $settingsKey)) 'the settings key is removed'
    Assert-That ($null -eq (Get-ItemProperty $runKey -Name $runValue -ErrorAction SilentlyContinue)) 'the UI no longer starts at sign-in'
    Assert-That (-not (Test-Path $startMenuFolder)) 'the Start menu folder is removed'
}

if ($failed) {
    Write-Host 'The installer test failed.' -ForegroundColor Red
    exit 1
}
Write-Host 'The installer test passed.' -ForegroundColor Green
