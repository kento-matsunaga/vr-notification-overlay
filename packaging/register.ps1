<#
.SYNOPSIS
    Registers the Sparse MSIX package with an external content location.
.DESCRIPTION
    Points AppxManifest.xml to the build output directory so that the
    prototype executable gets a Package Identity (required for UserNotificationListener).
.PARAMETER BuildOutput
    Path to the build output directory containing the prototype .exe.
    Defaults to the Release output of VRNotify.Integration.Prototype.
#>
param(
    [string]$BuildOutput
)

$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$manifestPath = Join-Path $scriptDir 'AppxManifest.xml'

if (-not $BuildOutput) {
    $BuildOutput = Join-Path $scriptDir '..\tests\VRNotify.Integration.Prototype\bin\Release\net8.0-windows10.0.19041.0\win-x64'
}

$resolved = Resolve-Path $BuildOutput -ErrorAction SilentlyContinue
$BuildOutput = if ($resolved) { $resolved.Path } else { $null }
if (-not $BuildOutput) {
    Write-Error "Build output directory not found. Build the prototype first:`n  dotnet build tests/VRNotify.Integration.Prototype -c Release"
    exit 1
}

# Validate manifest exists
if (-not (Test-Path $manifestPath)) {
    Write-Error "AppxManifest.xml not found at: $manifestPath"
    exit 1
}

# Validate Publisher matches certificate
$subject = 'CN=VRNotify-Dev'
$cert = Get-ChildItem -Path 'Cert:\LocalMachine\TrustedPeople' |
    Where-Object { $_.Subject -eq $subject }

if (-not $cert) {
    Write-Error "Certificate not found ($subject). Run create-cert.ps1 first (as admin)."
    exit 1
}

Write-Host "Manifest:      $manifestPath"
Write-Host "ExternalPath:  $BuildOutput"
Write-Host "Certificate:   $($cert.Thumbprint)"
Write-Host ""

# Copy manifest + placeholder to build output
Copy-Item $manifestPath -Destination $BuildOutput -Force
$placeholderSrc = Join-Path $scriptDir 'placeholder.png'
if (Test-Path $placeholderSrc) {
    Copy-Item $placeholderSrc -Destination $BuildOutput -Force
}

Write-Host "Registering Sparse Package..."
Add-AppxPackage -Register (Join-Path $BuildOutput 'AppxManifest.xml') -ExternalLocation $BuildOutput

Write-Host "[OK] Package registered successfully." -ForegroundColor Green
Write-Host "You can now run the prototype from: $BuildOutput"
