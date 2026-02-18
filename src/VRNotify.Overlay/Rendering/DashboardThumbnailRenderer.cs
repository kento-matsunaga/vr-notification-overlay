using SkiaSharp;

namespace VRNotify.Overlay.Rendering;

public static class DashboardThumbnailRenderer
{
    private const int Size = 64;

    private static readonly string FontFamily = ResolveFont();
    private static readonly SKTypeface CachedBoldTypeface =
        SKTypeface.FromFamilyName(FontFamily, SKFontStyleWeight.Bold,
            SKFontStyleWidth.Normal, SKFontStyleSlant.Upright) ?? SKTypeface.Default;

    public static byte[] RenderPng()
    {
        var info = new SKImageInfo(Size, Size, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;

        // Background
        using var bgPaint = new SKPaint
        {
            Color = new SKColor(0x00, 0x78, 0xD4),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(0, 0, Size, Size), 8f), bgPaint);

        // "VR" text
        using var textPaint = new SKPaint
        {
            Color = SKColors.White,
            TextSize = 24f,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = CachedBoldTypeface
        };
        canvas.DrawText("VR", Size / 2f, Size / 2f + textPaint.TextSize / 3f, textPaint);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static string ResolveFont()
    {
        using var test = SKTypeface.FromFamilyName("Yu Gothic UI");
        return test is not null && test.FamilyName == "Yu Gothic UI"
            ? "Yu Gothic UI"
            : "Segoe UI";
    }
}
