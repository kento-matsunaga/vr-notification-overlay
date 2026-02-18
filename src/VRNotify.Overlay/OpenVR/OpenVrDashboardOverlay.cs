using System.Runtime.InteropServices;
using OVRSharp;
using Serilog;
using SkiaSharp;
using Valve.VR;
using VRNotify.Domain.Configuration;
using VRNotify.Domain.VRDisplay;
using VRNotify.Overlay.Rendering;

namespace VRNotify.Overlay.OpenVR;

public sealed class OpenVrDashboardOverlay : IDashboardOverlay
{
    private OVRSharp.Overlay? _overlay;
    private readonly ILogger _logger;
    private readonly SkiaSettingsPanelRenderer _renderer = new();
    private Dictionary<string, SKRect> _hitAreas = new();
    private string? _texturePath;
    private string? _thumbnailPath;

    public bool IsAvailable { get; private set; }
    public event Action<string>? ButtonClicked;

    public OpenVrDashboardOverlay(ILogger logger)
    {
        _logger = logger.ForContext<OpenVrDashboardOverlay>();
    }

    public Task InitializeAsync(CancellationToken ct = default)
    {
        // Create dashboard overlay (OVRSharp dashboardOverlay parameter)
        _overlay = new OVRSharp.Overlay("vrnotify.dashboard", "VRNotify Settings", dashboardOverlay: true);
        _overlay.WidthInMeters = 1.5f;

        // Tell OpenVR the overlay pixel size so mouse events return pixel coordinates
        var mouseScale = new HmdVector2_t { v0 = SkiaSettingsPanelRenderer.PanelWidth, v1 = SkiaSettingsPanelRenderer.PanelHeight };
        Valve.VR.OpenVR.Overlay.SetOverlayMouseScale(_overlay.Handle, ref mouseScale);

        // Set thumbnail
        SetThumbnail();

        IsAvailable = true;
        _logger.Information("Dashboard overlay initialized");
        return Task.CompletedTask;
    }

    public void RenderPanel(DndMode dndMode, float opacity, double displayDurationSeconds, string statusText)
    {
        if (_overlay is null) return;

        var result = _renderer.Render(dndMode, opacity, displayDurationSeconds, statusText);
        _hitAreas = result.HitAreas;

        // Save as PNG and set texture
        _texturePath = Path.Combine(Path.GetTempPath(), "vrnotify_dashboard.png");
        SavePixelsAsPng(result.Pixels, SkiaSettingsPanelRenderer.PanelWidth, SkiaSettingsPanelRenderer.PanelHeight, _texturePath);
        _overlay.SetTextureFromFile(_texturePath);
    }

    public void PollEvents()
    {
        if (_overlay is null) return;

        var evt = new VREvent_t();
        while (Valve.VR.OpenVR.Overlay.PollNextOverlayEvent(
            _overlay.Handle, ref evt, (uint)Marshal.SizeOf<VREvent_t>()))
        {
            if (evt.eventType == (uint)EVREventType.VREvent_MouseButtonDown)
            {
                var mouseData = evt.data.mouse;
                HandleClick(mouseData.x, mouseData.y);
            }
        }
    }

    private void HandleClick(float x, float y)
    {
        // With MouseScale set, OpenVR returns pixel coordinates directly
        // Y axis is inverted (0 = bottom of overlay)
        float pixelX = x;
        float pixelY = SkiaSettingsPanelRenderer.PanelHeight - y;

        foreach (var (id, rect) in _hitAreas)
        {
            if (rect.Contains(pixelX, pixelY))
            {
                if (id.StartsWith("opacity_slider"))
                {
                    // Calculate opacity value from X position within slider
                    float normalized = Math.Clamp((pixelX - rect.Left) / rect.Width, 0f, 1f);
                    float opacityValue = 0.1f + normalized * 0.9f;
                    ButtonClicked?.Invoke($"opacity:{opacityValue:F2}");
                }
                else if (id.StartsWith("duration_slider"))
                {
                    float normalized = Math.Clamp((pixelX - rect.Left) / rect.Width, 0f, 1f);
                    double durationValue = 2.0 + normalized * 13.0;
                    ButtonClicked?.Invoke($"duration:{durationValue:F0}");
                }
                else
                {
                    ButtonClicked?.Invoke(id);
                }
                return;
            }
        }
    }

    private void SetThumbnail()
    {
        if (_overlay is null) return;

        var pngBytes = DashboardThumbnailRenderer.RenderPng();
        _thumbnailPath = Path.Combine(Path.GetTempPath(), "vrnotify_dashboard_thumb.png");
        File.WriteAllBytes(_thumbnailPath, pngBytes);

        var error = Valve.VR.OpenVR.Overlay.SetOverlayFromFile(
            _overlay.Handle + 1, // Dashboard thumbnail handle is main + 1
            _thumbnailPath);

        if (error != EVROverlayError.None)
            _logger.Warning("Failed to set dashboard thumbnail: {Error}", error);
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
            using var stream = File.Create(path);
            data.SaveTo(stream);
        }
        finally
        {
            handle.Free();
        }
    }

    public ValueTask DisposeAsync()
    {
        if (_overlay is not null)
        {
            _overlay.Destroy();
            _overlay = null;
        }

        IsAvailable = false;
        return ValueTask.CompletedTask;
    }
}
