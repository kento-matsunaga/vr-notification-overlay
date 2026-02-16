#Requires -RunAsAdministrator
<#
.SYNOPSIS
    VRNotify Portable Edition - First-time setup script.
.DESCRIPTION
    1. Creates a self-signed certificate (CN=VRNotify) in TrustedPeople store
    2. Registers the Sparse MSIX package for Package Identity
    This is required for Windows notification listener access.
    Run this script ONCE after extracting the ZIP.
#>

$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "=== VRNotify Portable Setup ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Certificate
Write-Host "[1/2] Checking certificate..." -ForegroundColor Yellow
$subject = 'CN=VRNotify'
$storeName = 'TrustedPeople'

$existing = Get-ChildItem -Path "Cert:\LocalMachine\$storeName" |
    Where-Object { $_.Subject -eq $subject }

if ($existing) {
    Write-Host "  Certificate already exists: $($existing.Thumbprint)" -ForegroundColor Green
} else {
    Write-Host "  Creating self-signed certificate ($subject)..."
    $cert = New-SelfSignedCertificate `
        -Type Custom `
        -Subject $subject `
        -KeyUsage DigitalSignature `
        -FriendlyName 'VRNotify Sparse Package' `
        -CertStoreLocation 'Cert:\CurrentUser\My' `
        -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3', '2.5.29.19={text}')

    $store = New-Object System.Security.Cryptography.X509Certificates.X509Store(
        $storeName, [System.Security.Cryptography.X509Certificates.StoreLocation]::LocalMachine)
    $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
    $store.Add($cert)
    $store.Close()

    Get-ChildItem "Cert:\CurrentUser\My\$($cert.Thumbprint)" | Remove-Item
    Write-Host "  Certificate installed: $($cert.Thumbprint)" -ForegroundColor Green
}

# Step 2: Sparse MSIX registration
Write-Host ""
Write-Host "[2/2] Registering Sparse MSIX package..." -ForegroundColor Yellow
$manifestPath = Join-Path $scriptDir "AppxManifest.xml"

if (-not (Test-Path $manifestPath)) {
    Write-Error "AppxManifest.xml not found at: $manifestPath"
    exit 1
}

try {
    $existingPkg = Get-AppxPackage -Name "VRNotify" -ErrorAction SilentlyContinue
    if ($existingPkg) {
        Write-Host "  Removing existing package..."
        Remove-AppxPackage $existingPkg.PackageFullName
    }

    Add-AppxPackage -Register $manifestPath -ExternalLocation $scriptDir
    Write-Host "  Package registered successfully" -ForegroundColor Green
} catch {
    Write-Error "Failed to register package: $_"
    Write-Host ""
    Write-Host "Please make sure:" -ForegroundColor Yellow
    Write-Host "  1. You are running this script as Administrator"
    Write-Host "  2. Windows 10 (Build 19041) or later"
    exit 1
}

Write-Host ""
Write-Host "=== Setup Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "You can now launch VRNotify from the Start Menu." -ForegroundColor White
Write-Host ""
Write-Host "NOTE: VRNotify must be launched via the Start Menu shortcut" -ForegroundColor Yellow
Write-Host "      (not by double-clicking the .exe directly)" -ForegroundColor Yellow
Write-Host "      to enable Windows notification access." -ForegroundColor Yellow
