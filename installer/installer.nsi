; VRNotify NSIS Installer Script
; Based on OpenVR Advanced Settings approach: NSIS + Sparse MSIX

!include "MUI2.nsh"
!include "nsDialogs.nsh"
!include "LogicLib.nsh"

; ---- General ----
Name "VRNotify"
OutFile "..\dist\VRNotify-1.0.0-Installer.exe"
InstallDir "$PROGRAMFILES64\VRNotify"
RequestExecutionLevel admin
Unicode True

; ---- Version Info ----
!define VERSION "1.0.0"
!define PUBLISHER "VRNotify"
VIProductVersion "1.0.0.0"
VIAddVersionKey "ProductName" "VRNotify"
VIAddVersionKey "ProductVersion" "${VERSION}"
VIAddVersionKey "FileDescription" "VRNotify Installer"
VIAddVersionKey "FileVersion" "${VERSION}"
VIAddVersionKey "LegalCopyright" "Copyright (c) 2026 VRNotify Contributors"

; ---- MUI Settings ----
!define MUI_ABORTWARNING
!define MUI_WELCOMEPAGE_TITLE "VRNotify ${VERSION}"
!define MUI_WELCOMEPAGE_TEXT "This wizard will install VRNotify - VR Notification Overlay.$\r$\n$\r$\nVRNotify displays your Windows notifications inside VR via SteamVR.$\r$\n$\r$\nRequirements:$\r$\n- Windows 10/11 (64-bit)$\r$\n- SteamVR"

; ---- Pages ----
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

; ---- Install Section ----
Section "Install"
    SetOutPath "$INSTDIR"

    ; Copy application files (includes AppxManifest.xml, placeholder.png, manifest.vrmanifest)
    File /r "..\publish\*.*"

    ; Create uninstaller
    WriteUninstaller "$INSTDIR\Uninstall.exe"

    ; --- Certificate ---
    DetailPrint "Creating self-signed certificate..."
    nsExec::ExecToLog 'powershell.exe -ExecutionPolicy Bypass -Command "\
        $$subject = \"CN=VRNotify\"; \
        $$existing = Get-ChildItem -Path \"Cert:\LocalMachine\TrustedPeople\" | Where-Object { $$_.Subject -eq $$subject }; \
        if (-not $$existing) { \
            $$cert = New-SelfSignedCertificate -Type Custom -Subject $$subject -KeyUsage DigitalSignature -FriendlyName \"VRNotify Sparse Package\" -CertStoreLocation \"Cert:\CurrentUser\My\" -TextExtension @(\"2.5.29.37={text}1.3.6.1.5.5.7.3.3\", \"2.5.29.19={text}\"); \
            $$store = New-Object System.Security.Cryptography.X509Certificates.X509Store(\"TrustedPeople\", [System.Security.Cryptography.X509Certificates.StoreLocation]::LocalMachine); \
            $$store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite); \
            $$store.Add($$cert); \
            $$store.Close(); \
            Get-ChildItem \"Cert:\CurrentUser\My\$$($$cert.Thumbprint)\" | Remove-Item; \
            Write-Host \"Certificate installed: $$($$cert.Thumbprint)\" \
        } else { \
            Write-Host \"Certificate already exists: $$($$existing.Thumbprint)\" \
        }"'

    ; --- Sparse MSIX Registration ---
    DetailPrint "Registering Sparse MSIX package..."
    nsExec::ExecToLog 'powershell.exe -ExecutionPolicy Bypass -Command "\
        Add-AppxPackage -Register \"$INSTDIR\AppxManifest.xml\" -ExternalLocation \"$INSTDIR\""'

    ; --- Registry (Add/Remove Programs) ---
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\VRNotify" \
        "DisplayName" "VRNotify"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\VRNotify" \
        "UninstallString" "$\"$INSTDIR\Uninstall.exe$\""
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\VRNotify" \
        "DisplayVersion" "${VERSION}"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\VRNotify" \
        "Publisher" "${PUBLISHER}"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\VRNotify" \
        "InstallLocation" "$INSTDIR"
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\VRNotify" \
        "NoModify" 1
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\VRNotify" \
        "NoRepair" 1

    ; --- Start Menu Shortcut ---
    ; Launch via shell:AppsFolder AUMID so the process gets Package Identity
    CreateDirectory "$SMPROGRAMS\VRNotify"
    CreateShortcut "$SMPROGRAMS\VRNotify\VRNotify.lnk" "explorer.exe" \
        "shell:AppsFolder\VRNotify_qpjsvqwttpe7c!VRNotifyApp"
    CreateShortcut "$SMPROGRAMS\VRNotify\Uninstall.lnk" "$INSTDIR\Uninstall.exe"

    DetailPrint "Installation complete!"
SectionEnd

; ---- Uninstall Section ----
Section "Uninstall"
    ; --- Remove MSIX Package ---
    DetailPrint "Removing Sparse MSIX package..."
    nsExec::ExecToLog 'powershell.exe -ExecutionPolicy Bypass -Command "\
        $$pkg = Get-AppxPackage -Name \"VRNotify\" -ErrorAction SilentlyContinue; \
        if ($$pkg) { Remove-AppxPackage $$pkg.PackageFullName }"'

    ; --- Remove SteamVR manifest ---
    ; SteamVR auto-discovers manifests, no explicit unregister needed

    ; --- Remove Files ---
    RMDir /r "$INSTDIR"

    ; --- Remove Start Menu ---
    RMDir /r "$SMPROGRAMS\VRNotify"

    ; --- Remove Registry ---
    DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\VRNotify"

    DetailPrint "Uninstallation complete!"
SectionEnd
