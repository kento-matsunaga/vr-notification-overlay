using VRNotify.Domain.NotificationProcessing;
using VRNotify.Domain.SourceConnection;
using VRNotify.Domain.VRDisplay;
using VRNotify.Infrastructure.Queuing;
using VRNotify.Infrastructure.Windows;
using VRNotify.Overlay.OpenVR;

Console.WriteLine("=== VRNotify Integration Prototype ===");
Console.WriteLine("  Windows Notification → VR Headset Display");
Console.WriteLine();

// [1/6] Verify Package Identity
Console.WriteLine("[1/6] Checking Package Identity...");
try
{
    var package = Windows.ApplicationModel.Package.Current;
    Console.WriteLine($"  Package: {package.Id.Name} v{package.Id.Version.Major}.{package.Id.Version.Minor}.{package.Id.Version.Build}");
}
catch (InvalidOperationException)
{
    Console.WriteLine("  ERROR: No Package Identity found.");
    Console.WriteLine("  Run the following from the repo root:");
    Console.WriteLine("    1. powershell -File packaging/create-cert.ps1       (as admin, once)");
    Console.WriteLine("    2. dotnet build tests/VRNotify.Integration.Prototype -c Release");
    Console.WriteLine("    3. powershell -File packaging/register.ps1");
    Console.WriteLine();
    Console.WriteLine("  Press Enter to exit...");
    Console.ReadLine();
    return;
}

// [2/6] Initialize OpenVR overlay
Console.WriteLine("[2/6] Initializing OpenVR overlay...");
await using var overlayManager = new OpenVrOverlayManager();
try
{
    await overlayManager.InitializeAsync();
    Console.WriteLine("  Overlay initialized successfully.");
}
catch (Exception ex)
{
    Console.WriteLine($"  ERROR: Failed to initialize OpenVR: {ex.Message}");
    Console.WriteLine("  Make sure SteamVR is running and a VR headset is connected.");
    Console.WriteLine("  Press Enter to exit...");
    Console.ReadLine();
    return;
}

// [3/6] Set up notification queue and display slot
Console.WriteLine("[3/6] Setting up notification queue and display slot...");
var queue = new ChannelNotificationQueue();
var slot = new DisplaySlot(0);
Console.WriteLine("  Queue capacity: 100, DisplaySlot: 0");

// [4/6] Connect Windows notification adapter
Console.WriteLine("[4/6] Connecting Windows Notification Listener...");
await using var adapter = new WindowsNotificationAdapter();

adapter.NotificationReceived += async evt =>
{
    Console.WriteLine();
    Console.WriteLine($"  [CAPTURED] {evt.Timestamp:HH:mm:ss} {evt.Sender.Name}: {evt.Content.TruncatedText}");

    var card = new NotificationCard(
        cardId: Guid.NewGuid(),
        originEventId: evt.EventId,
        sourceType: SourceType.WindowsNotification,
        priority: Priority.Low,
        title: evt.Channel.Name,
        body: evt.Content.TruncatedText,
        senderDisplay: evt.Sender.Name,
        senderAvatarUrl: null,
        displayDuration: TimeSpan.FromSeconds(5));

    await queue.EnqueueAsync(card);
    Console.WriteLine($"  [QUEUED] CardId={card.CardId:N} (queue count: {queue.Count})");
};

adapter.ConnectionStateChanged += state =>
{
    Console.WriteLine($"  Connection state: {state}");
    return Task.CompletedTask;
};

var dummyCredential = new EncryptedCredential(Array.Empty<byte>(), Array.Empty<byte>());
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

// [5/6] Start display loop
Console.WriteLine("[5/6] Starting display loop...");
using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var displayLoop = Task.Run(async () =>
{
    try
    {
        await foreach (var card in queue.DequeueAllAsync(cts.Token))
        {
            Console.WriteLine($"  [DISPLAY] Showing: {card.SenderDisplay} - {card.Body}");
            await overlayManager.ShowNotificationAsync(card, slot);

            await Task.Delay(card.DisplayDuration, cts.Token);

            await overlayManager.HideSlotAsync(slot);
            Console.WriteLine($"  [HIDDEN] Card dismissed after {card.DisplayDuration.TotalSeconds}s");
        }
    }
    catch (OperationCanceledException)
    {
        // Expected on shutdown
    }
}, cts.Token);

// [6/6] Monitor loop
Console.WriteLine("[6/6] Monitoring for 120 seconds (Ctrl+C to stop)...");
Console.WriteLine();
Console.WriteLine("  Send a test notification with PowerShell:");
Console.WriteLine("    [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null");
Console.WriteLine("    $xml = [Windows.UI.Notifications.ToastNotificationManager]::GetTemplateContent([Windows.UI.Notifications.ToastTemplateType]::ToastText02)");
Console.WriteLine("    $texts = $xml.GetElementsByTagName('text')");
Console.WriteLine("    $texts[0].AppendChild($xml.CreateTextNode('Test Title')) | Out-Null");
Console.WriteLine("    $texts[1].AppendChild($xml.CreateTextNode('Hello from PowerShell!')) | Out-Null");
Console.WriteLine("    [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('VRNotify.Test').Show([Windows.UI.Notifications.ToastNotification]::new($xml))");
Console.WriteLine();

try
{
    for (int i = 120; i > 0; i--)
    {
        Console.Write($"\r  Remaining: {i}s | Queue: {queue.Count} (Ctrl+C to stop)  ");
        await Task.Delay(1000, cts.Token);
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine();
    Console.WriteLine("  Stopped by user.");
}

// Cleanup
cts.Cancel();
try { await displayLoop; } catch (OperationCanceledException) { }

if (slot.IsOccupied)
    await overlayManager.HideSlotAsync(slot);

Console.WriteLine();
Console.WriteLine("Disconnecting...");
await adapter.DisconnectAsync();
Console.WriteLine("Done.");
