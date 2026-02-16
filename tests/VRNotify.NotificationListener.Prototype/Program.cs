using VRNotify.Domain.SourceConnection;
using VRNotify.Infrastructure.Windows;

Console.WriteLine("=== VRNotify Notification Listener Prototype ===");
Console.WriteLine();

// [1/4] Verify Package Identity
Console.WriteLine("[1/4] Checking Package Identity...");
try
{
    var package = Windows.ApplicationModel.Package.Current;
    Console.WriteLine($"  Package: {package.Id.Name} v{package.Id.Version.Major}.{package.Id.Version.Minor}.{package.Id.Version.Build}");
}
catch (InvalidOperationException)
{
    Console.WriteLine("  ERROR: No Package Identity found.");
    Console.WriteLine("  Run the following from the packaging/ directory (as admin):");
    Console.WriteLine("    1. powershell -File create-cert.ps1");
    Console.WriteLine("    2. dotnet build tests/VRNotify.NotificationListener.Prototype -c Release");
    Console.WriteLine("    3. powershell -File register.ps1");
    Console.WriteLine();
    Console.WriteLine("  Press Enter to exit...");
    Console.ReadLine();
    return;
}

// [2/4] Initialize adapter
Console.WriteLine("[2/4] Initializing Windows Notification Listener...");
await using var adapter = new WindowsNotificationAdapter();

var dummyCredential = new EncryptedCredential(Array.Empty<byte>(), Array.Empty<byte>());

adapter.NotificationReceived += evt =>
{
    Console.WriteLine();
    Console.WriteLine($"  [{evt.Timestamp:HH:mm:ss}] {evt.Sender.Name}");
    Console.WriteLine($"    Title: {evt.Channel.Name}");
    Console.WriteLine($"    Body:  {evt.Content.TruncatedText}");
    Console.WriteLine();
    return Task.CompletedTask;
};

adapter.ConnectionStateChanged += state =>
{
    Console.WriteLine($"  Connection state: {state}");
    return Task.CompletedTask;
};

try
{
    await adapter.ConnectAsync(dummyCredential);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"  ERROR: {ex.Message}");
    Console.WriteLine("  Make sure notification access is allowed in Windows Settings:");
    Console.WriteLine("    Settings > Privacy & Security > Notifications");
    Console.WriteLine();
    Console.WriteLine("  Press Enter to exit...");
    Console.ReadLine();
    return;
}

// [3/4] Listening
Console.WriteLine("[3/4] Listening for notifications...");
Console.WriteLine("  Send a test notification with PowerShell:");
Console.WriteLine("    [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null");
Console.WriteLine("    $xml = [Windows.UI.Notifications.ToastNotificationManager]::GetTemplateContent([Windows.UI.Notifications.ToastTemplateType]::ToastText02)");
Console.WriteLine("    $texts = $xml.GetElementsByTagName('text')");
Console.WriteLine("    $texts[0].AppendChild($xml.CreateTextNode('Test Title')) | Out-Null");
Console.WriteLine("    $texts[1].AppendChild($xml.CreateTextNode('Hello from PowerShell!')) | Out-Null");
Console.WriteLine("    [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('VRNotify.Test').Show([Windows.UI.Notifications.ToastNotification]::new($xml))");
Console.WriteLine();

// [4/4] Monitor loop
Console.WriteLine("[4/4] Monitoring for 120 seconds (Ctrl+C to stop)...");
Console.WriteLine();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    for (int i = 120; i > 0; i--)
    {
        Console.Write($"\r  Remaining: {i}s (Ctrl+C to stop)  ");
        await Task.Delay(1000, cts.Token);
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine();
    Console.WriteLine("  Stopped by user.");
}

Console.WriteLine();
Console.WriteLine("Disconnecting...");
await adapter.DisconnectAsync();
Console.WriteLine("Done.");
