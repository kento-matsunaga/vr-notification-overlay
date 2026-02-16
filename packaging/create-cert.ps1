#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Creates a self-signed certificate for Sparse MSIX package signing.
.DESCRIPTION
    The certificate Subject must match the Publisher in AppxManifest.xml (CN=VRNotify-Dev).
    Installs to TrustedPeople store so Windows trusts the package.
#>

$ErrorActionPreference = 'Stop'
$subject = 'CN=VRNotify'
$storeName = 'TrustedPeople'

# Check for existing cert
$existing = Get-ChildItem -Path "Cert:\LocalMachine\$storeName" |
    Where-Object { $_.Subject -eq $subject }

if ($existing) {
    Write-Host "[OK] Certificate already exists: $($existing.Thumbprint)" -ForegroundColor Green
    exit 0
}

Write-Host "Creating self-signed certificate ($subject)..."
$cert = New-SelfSignedCertificate `
    -Type Custom `
    -Subject $subject `
    -KeyUsage DigitalSignature `
    -FriendlyName 'VRNotify Sparse Package' `
    -CertStoreLocation 'Cert:\CurrentUser\My' `
    -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3', '2.5.29.19={text}')

Write-Host "Moving to TrustedPeople store..."
$store = New-Object System.Security.Cryptography.X509Certificates.X509Store(
    $storeName, [System.Security.Cryptography.X509Certificates.StoreLocation]::LocalMachine)
$store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
$store.Add($cert)
$store.Close()

# Remove from personal store
Get-ChildItem "Cert:\CurrentUser\My\$($cert.Thumbprint)" | Remove-Item

Write-Host "[OK] Certificate installed: $($cert.Thumbprint)" -ForegroundColor Green
