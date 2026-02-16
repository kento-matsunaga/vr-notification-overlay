<#
.SYNOPSIS
    Unregisters the VRNotify Sparse MSIX package.
#>

$ErrorActionPreference = 'Stop'
$packageName = 'VRNotify-Dev'

$pkg = Get-AppxPackage -Name $packageName -ErrorAction SilentlyContinue
if ($pkg) {
    Write-Host "Removing package: $($pkg.PackageFullName)"
    Remove-AppxPackage -Package $pkg.PackageFullName
    Write-Host "[OK] Package removed." -ForegroundColor Green
} else {
    Write-Host "Package '$packageName' is not registered." -ForegroundColor Yellow
}
