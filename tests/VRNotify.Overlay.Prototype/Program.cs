using Serilog;
using VRNotify.Domain.NotificationProcessing;
using VRNotify.Domain.SourceConnection;
using VRNotify.Domain.VRDisplay;
using VRNotify.Overlay.OpenVR;

Console.WriteLine("=== VRNotify Overlay Prototype ===");
Console.WriteLine();

Console.WriteLine("[1/4] Creating notification card...");
var card = new NotificationCard(
    cardId: Guid.NewGuid(),
    originEventId: Guid.NewGuid(),
    sourceType: SourceType.Discord,
    priority: Priority.High,
    title: "#general",
    body: "Hello from VRNotify! This is a test notification.",
    senderDisplay: "TestUser",
    senderAvatarUrl: null,
    displayDuration: TimeSpan.FromSeconds(10));
Console.WriteLine("  Card created: Discord / High / TestUser / #general");

Console.WriteLine("[2/4] Initializing OpenVR overlay...");
await using var manager = new OpenVrOverlayManager(Log.Logger);
try
{
    await manager.InitializeAsync();
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

Console.WriteLine("[3/4] Displaying notification in VR...");
var slot = new DisplaySlot(0);
try
{
    await manager.ShowNotificationAsync(card, slot);
    Console.WriteLine("  Notification displayed! Check your VR headset.");
}
catch (Exception ex)
{
    Console.WriteLine($"  ERROR: Failed to display notification: {ex.Message}");
    Console.WriteLine("  Press Enter to exit...");
    Console.ReadLine();
    return;
}

Console.WriteLine("[4/4] Overlay is active. Keeping alive for 30 seconds...");
Console.WriteLine("  Look in your VR headset now!");
Console.WriteLine();

// Wait 30 seconds so user can check VR headset
for (int i = 30; i > 0; i--)
{
    Console.Write($"\r  Shutting down in {i} seconds... ");
    await Task.Delay(1000);
}
Console.WriteLine();
Console.WriteLine("Shutting down...");
