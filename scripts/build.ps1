# Copyright (c) Microsoft. All rights reserved.
# Build script for Test Platform.

[CmdletBinding()]
Param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Debug", "Release")]
    [Alias("c")]
    [System.String] $Configuration = "Debug"
)

#
# Variables
#
Write-Verbose "Setup environment variables."
$env:LE_ROOT_DIR = (Get-Item (Split-Path $MyInvocation.MyCommand.Path)).Parent.FullName
$env:LE_TOOLS_DIR = Join-Path $env:LE_ROOT_DIR "tools"
$env:LE_PACKAGES_DIR = Join-Path $env:LE_ROOT_DIR "packages"

#
# Dotnet configuration
#
# Disable first run since we want to control all package sources 
Write-Verbose "Setup dotnet configuration."
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 1 
$env:NUGET_PACKAGES = $env:LE_PACKAGES_DIR
$env:DOTNET_CLI_VERSION = "latest"


#
# Build configuration
#
$LEB_Solution = "Appveyor.TestLogger.sln"
$LEB_TestProject = Join-Path $env:LE_ROOT_DIR "Appveyor.TestLogger.Tests\Appveyor.TestLogger.Tests.csproj"


function Write-Log ([string] $message)
{
    $currentColor = $Host.UI.RawUI.ForegroundColor
    $Host.UI.RawUI.ForegroundColor = "Green"
    if ($message)
    {
        Write-Output "... $message"
    }
    $Host.UI.RawUI.ForegroundColor = $currentColor
}

function Install-DotNetCli
{
    $timer = Start-Timer
    Write-Log "Install-DotNetCli: Get dotnet-install.ps1 script..."
    $dotnetInstallRemoteScript = "https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/dotnet-install.ps1"
    $dotnetInstallScript = Join-Path $env:LE_TOOLS_DIR "dotnet-install.ps1"
    if (-not (Test-Path $env:LE_TOOLS_DIR)) {
        New-Item $env:LE_TOOLS_DIR -Type Directory | Out-Null
    }

    $dotnet_dir= Join-Path $env:LE_TOOLS_DIR "dotnet"

    if (-not (Test-Path $dotnet_dir)) {
        New-Item $dotnet_dir -Type Directory | Out-Null
    }

    (New-Object System.Net.WebClient).DownloadFile($dotnetInstallRemoteScript, $dotnetInstallScript)

    if (-not (Test-Path $dotnetInstallScript)) {
        Write-Error "Failed to download dotnet install script."
    }

    Unblock-File $dotnetInstallScript

    Write-Log "Install-DotNetCli: Get the latest dotnet cli toolset..."
    $dotnetInstallPath = Join-Path $env:LE_TOOLS_DIR "dotnet"
    New-Item -ItemType directory -Path $dotnetInstallPath -Force | Out-Null
    & $dotnetInstallScript -InstallDir $dotnetInstallPath -NoPath -Version $env:DOTNET_CLI_VERSION

    # Uncomment to pull in additional shared frameworks.
    # This is added to get netcoreapp1.1 shared components.
    #& $dotnetInstallScript -InstallDir $dotnetInstallPath -SharedRuntime -Version '1.1.0' -Channel 'release/1.1.0'

    Write-Log "Install-DotNetCli: Complete. {$(Get-ElapsedTime($timer))}"
}

function Restore-Package
{
    $timer = Start-Timer
    $dotnetExe = Get-DotNetPath

    Write-Log ".. .. Restore-Package: Source: $LEB_Solution"
    & $dotnetExe restore $LEB_Solution --packages $env:LE_PACKAGES_DIR -v:minimal -warnaserror
    Write-Log ".. .. Restore-Package: Complete."

    if ($lastExitCode -ne 0) {
        Set-ScriptFailed
    }

    Write-Log "Restore-Package: Complete. {$(Get-ElapsedTime($timer))}"
}

function Run-Test
{
    $timer = Start-Timer
    $dotnetExe = Get-DotNetPath

    $testAdapterPath = Join-Path $env:LE_ROOT_DIR "Appveyor.TestLogger.Tests\bin\$Configuration\netcoreapp1.0"

    Write-Log ".. .. Run-Test: Source: $LEB_TestProject"
    & $dotnetExe test $LEB_TestProject --test-adapter-path $testAdapterPath --configuration:$Configuration --logger:Appveyor

    Write-Log "Run-Test: Complete. {$(Get-ElapsedTime($timer))}"
}

#
# Helper functions
#
function Get-DotNetPath
{
    $dotnetPath = Join-Path $env:LE_TOOLS_DIR "dotnet\dotnet.exe"
    if (-not (Test-Path $dotnetPath)) {
        Write-Error "Dotnet.exe not found at $dotnetPath. Did the dotnet cli installation succeed?"
    }

    return $dotnetPath
}

function Start-Timer
{
    return [System.Diagnostics.Stopwatch]::StartNew()
}

function Get-ElapsedTime([System.Diagnostics.Stopwatch] $timer)
{
    $timer.Stop()
    return $timer.Elapsed
}


# Execute build
$timer = Start-Timer
Write-Log "Build started: args = '$args'"
Write-Log "Test platform environment variables: "
Get-ChildItem env: | Where-Object -FilterScript { $_.Name.StartsWith("LE_") } | Format-Table
Write-Log "Test platform build variables: "
Get-Variable | Where-Object -FilterScript { $_.Name.StartsWith("LEB_") } | Format-Table
Install-DotNetCli
Restore-Package
Run-Test
Write-Log "Build complete. {$(Get-ElapsedTime($timer))}"
if ($Script:ScriptFailed) { Exit 1 } else { Exit 0 }
