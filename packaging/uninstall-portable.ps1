#Requires -RunAsAdministrator
<#
.SYNOPSIS
    VRNotify Portable Edition - Uninstall script.
.DESCRIPTION
    Removes the Sparse MSIX package registration.
    Certificate is left in place (harmless).
#>

$ErrorActionPreference = 'Stop'

Write-Host "=== VRNotify Portable Uninstall ===" -ForegroundColor Cyan
Write-Host ""

$pkg = Get-AppxPackage -Name "VRNotify" -ErrorAction SilentlyContinue
if ($pkg) {
    Write-Host "Removing Sparse MSIX package..."
    Remove-AppxPackage $pkg.PackageFullName
    Write-Host "Package removed." -ForegroundColor Green
} else {
    Write-Host "No VRNotify package found." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "You can now delete the VRNotify folder." -ForegroundColor White
Write-Host "The self-signed certificate (CN=VRNotify) remains in TrustedPeople store." -ForegroundColor Gray
Write-Host "To remove it manually: certlm.msc > Trusted People > Certificates > VRNotify" -ForegroundColor Gray
