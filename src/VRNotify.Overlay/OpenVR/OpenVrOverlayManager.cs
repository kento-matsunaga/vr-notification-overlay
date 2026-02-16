using System.Runtime.InteropServices;
using OVRSharp;
using SkiaSharp;
using Valve.VR;
using VRNotify.Domain.NotificationProcessing;
using VRNotify.Domain.VRDisplay;
using VRNotify.Overlay.Rendering;

namespace VRNotify.Overlay.OpenVR;

public sealed class OpenVrOverlayManager : IOverlayManager
{
    private const int TextureWidth = 512;
    private const int TextureHeight = 128;
    private const float OverlayWidthMeters = 0.3f;

    private OVRSharp.Application? _application;
    private OVRSharp.Overlay? _overlay;
    private readonly SkiaNotificationRenderer _renderer = new();
    private string? _texturePath;

    public bool IsAvailable { get; private set; }

    public Task InitializeAsync(CancellationToken ct = default)
    {
        _application = new OVRSharp.Application(OVRSharp.Application.ApplicationType.Overlay);

        _overlay = new OVRSharp.Overlay("vrnotify.main", "VRNotify");
        _overlay.WidthInMeters = OverlayWidthMeters;
        _overlay.Alpha = 1.0f;

        // Set HMD-relative position using OpenVR API directly
        var transform = new HmdMatrix34_t();
        // Identity rotation
        transform.m0 = 1f; transform.m5 = 1f; transform.m10 = 1f;
        // Position: centered, slightly above, 0.8m in front
        transform.m3 = 0f;      // X: center
        transform.m7 = 0.1f;    // Y: slightly above eye level
        transform.m11 = -0.8f;  // Z: 0.8m in front (negative Z is forward)
        Valve.VR.OpenVR.Overlay.SetOverlayTransformTrackedDeviceRelative(
            _overlay.Handle,
            Valve.VR.OpenVR.k_unTrackedDeviceIndex_Hmd,
            ref transform);

        IsAvailable = true;
        return Task.CompletedTask;
    }

    public Task ShowNotificationAsync(NotificationCard card, DisplaySlot slot, CancellationToken ct = default)
    {
        if (_overlay is null)
            throw new InvalidOperationException("Overlay not initialized. Call InitializeAsync first.");

        // Render the notification card to RGBA pixels
        var pixels = _renderer.RenderCard(card, TextureWidth, TextureHeight);

        // Save as PNG and use SetTextureFromFile for reliable texture loading
        _texturePath = Path.Combine(Path.GetTempPath(), "vrnotify_overlay.png");
        SavePixelsAsPng(pixels, TextureWidth, TextureHeight, _texturePath);
        _overlay.SetTextureFromFile(_texturePath);

        _overlay.Show();
        slot.Assign(card);
        card.MarkAsDisplayed();

        return Task.CompletedTask;
    }

    private static void SavePixelsAsPng(byte[] rgbaPixels, int width, int height, string path)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(info);
        var handle = GCHandle.Alloc(rgbaPixels, GCHandleType.Pinned);
        try
        {
            bitmap.InstallPixels(info, handle.AddrOfPinnedObject(), info.RowBytes);
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite(path);
            data.SaveTo(stream);
        }
        finally
        {
            handle.Free();
        }
    }

    public Task HideSlotAsync(DisplaySlot slot, CancellationToken ct = default)
    {
        _overlay?.Hide();
        slot.Release();
        return Task.CompletedTask;
    }

    public Task UpdatePositionAsync(DisplayPosition position, CancellationToken ct = default)
    {
        // For prototype, only HmdTop is supported
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        if (_overlay is not null)
        {
            _overlay.Destroy();
            _overlay = null;
        }

        if (_application is not null)
        {
            _application.Shutdown();
            _application = null;
        }

        IsAvailable = false;
        return ValueTask.CompletedTask;
    }
}
