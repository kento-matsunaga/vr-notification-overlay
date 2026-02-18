using System.Diagnostics;

// VRNotify Launcher
// SteamVR auto_launch invokes this exe, which then launches
// VRNotify.Desktop via shell:AppsFolder to ensure Package Identity.

try
{
    Process.Start(new ProcessStartInfo
    {
        FileName = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"),
        Arguments = @"shell:AppsFolder\VRNotify_qpjsvqwttpe7c!VRNotifyApp",
        UseShellExecute = false
    });
}
catch
{
    // Silent failure — SteamVR will handle the non-response gracefully
}
