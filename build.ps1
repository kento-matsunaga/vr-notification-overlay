<#
.SYNOPSIS
    Build script for VRNotify release packaging.
.DESCRIPTION
    1. Runs dotnet publish (self-contained, single file)
    2. Copies packaging assets, removes PDBs
    3. Creates NSIS installer (.exe)
    4. Creates portable ZIP (.zip) with setup scripts
.PARAMETER Configuration
    Build configuration (Release/Debug). Default: Release
.PARAMETER SkipNsis
    Skip NSIS installer creation (useful if NSIS is not installed)
#>
param(
    [string]$Configuration = "Release",
    [switch]$SkipNsis
)

$ErrorActionPreference = 'Stop'
$rootDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$publishDir = Join-Path $rootDir 'publish'
$distDir = Join-Path $rootDir 'dist'
$desktopProj = Join-Path $rootDir 'src\VRNotify.Desktop\VRNotify.Desktop.csproj'

Write-Host "=== VRNotify Build Script ===" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration"
Write-Host ""

# Step 1: Clean
Write-Host "[1/5] Cleaning..." -ForegroundColor Yellow
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
if (Test-Path $distDir) { Remove-Item $distDir -Recurse -Force }
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
New-Item -ItemType Directory -Path $distDir -Force | Out-Null

# Step 2: Publish
Write-Host "[2/5] Publishing VRNotify.Desktop..." -ForegroundColor Yellow
dotnet publish $desktopProj -c $Configuration -o $publishDir
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed"
    exit 1
}
Write-Host "  Published to: $publishDir" -ForegroundColor Green

# Step 3: Copy packaging assets and clean up
Write-Host "[3/5] Preparing release files..." -ForegroundColor Yellow
Copy-Item (Join-Path $rootDir 'packaging\AppxManifest.xml') -Destination $publishDir -Force
Copy-Item (Join-Path $rootDir 'packaging\placeholder.png') -Destination $publishDir -Force
Copy-Item (Join-Path $rootDir 'installer\manifest.vrmanifest') -Destination $publishDir -Force

# Remove PDB debug symbols (not needed for release)
Get-ChildItem -Path $publishDir -Filter "*.pdb" | Remove-Item -Force
Write-Host "  Assets copied, PDBs removed" -ForegroundColor Green

# Step 4: NSIS Installer
if ($SkipNsis) {
    Write-Host "[4/5] Skipping NSIS installer (-SkipNsis)" -ForegroundColor Yellow
} else {
    Write-Host "[4/5] Creating NSIS installer..." -ForegroundColor Yellow
    $makensis = $null

    # Check common NSIS install locations
    $nsisLocations = @(
        "C:\Program Files (x86)\NSIS\makensis.exe",
        "C:\Program Files\NSIS\makensis.exe"
    )
    foreach ($loc in $nsisLocations) {
        if (Test-Path $loc) {
            $makensis = $loc
            break
        }
    }

    if ($makensis) {
        $nsiScript = Join-Path $rootDir 'installer\installer.nsi'
        & $makensis $nsiScript
        if ($LASTEXITCODE -ne 0) {
            Write-Error "NSIS build failed"
            exit 1
        }
        Write-Host "  Installer created in: $distDir" -ForegroundColor Green
    } else {
        Write-Host "  NSIS not found. Skipping installer creation." -ForegroundColor Yellow
        Write-Host "  Install NSIS: winget install NSIS.NSIS" -ForegroundColor Yellow
    }
}

# Step 5: Portable ZIP (with setup/uninstall scripts)
Write-Host "[5/5] Creating portable ZIP..." -ForegroundColor Yellow

# Temporarily copy portable-only scripts and docs
$portableScripts = @(
    (Join-Path $rootDir 'packaging\create-cert.ps1'),
    (Join-Path $rootDir 'packaging\setup-portable.ps1'),
    (Join-Path $rootDir 'packaging\uninstall-portable.ps1'),
    (Join-Path $rootDir 'README.md'),
    (Join-Path $rootDir 'LICENSE')
)
$tempFiles = @()
foreach ($script in $portableScripts) {
    if (Test-Path $script) {
        $dest = Join-Path $publishDir (Split-Path -Leaf $script)
        Copy-Item $script -Destination $dest -Force
        $tempFiles += $dest
    }
}

$zipPath = Join-Path $distDir "VRNotify-1.0.0-Portable.zip"
Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath -Force
Write-Host "  ZIP created: $zipPath" -ForegroundColor Green

# Clean up portable-only scripts from publish/
foreach ($f in $tempFiles) {
    if (Test-Path $f) { Remove-Item $f -Force }
}

# Copy README to dist/ for reference
Copy-Item (Join-Path $rootDir 'README.md') -Destination $distDir -Force

Write-Host ""
Write-Host "=== Build Complete ===" -ForegroundColor Cyan
Write-Host "Dist directory: $distDir"
Get-ChildItem $distDir | ForEach-Object {
    $size = "{0:N1} MB" -f ($_.Length / 1MB)
    Write-Host "  $($_.Name) ($size)"
}
