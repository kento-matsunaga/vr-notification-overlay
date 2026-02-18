using SkiaSharp;
using VRNotify.Domain.Configuration;

namespace VRNotify.Overlay.Rendering;

public sealed class SkiaSettingsPanelRenderer
{
    public const int PanelWidth = 1024;
    public const int PanelHeight = 768;

    private static readonly SKColor BackgroundColor = new(0x1a, 0x1a, 0x2e, 0xF0);
    private static readonly SKColor HeaderColor = new(0x00, 0x78, 0xD4);
    private static readonly SKColor ButtonColor = new(0x33, 0x33, 0x50);
    private static readonly SKColor ButtonActiveColor = new(0x00, 0x78, 0xD4);
    private static readonly SKColor SliderTrackColor = new(0x44, 0x44, 0x60);
    private static readonly SKColor SliderFillColor = new(0x00, 0x78, 0xD4);
    private static readonly SKColor TextColor = SKColors.White;
    private static readonly SKColor SubTextColor = new(0xAA, 0xAA, 0xAA);
    private static readonly SKColor StatusGreenColor = new(0x44, 0xBB, 0x44);

    private static readonly string FontFamily = ResolveFont();
    private static readonly SKTypeface NormalTypeface =
        SKTypeface.FromFamilyName(FontFamily, SKFontStyleWeight.Normal,
            SKFontStyleWidth.Normal, SKFontStyleSlant.Upright) ?? SKTypeface.Default;
    private static readonly SKTypeface SemiBoldTypeface =
        SKTypeface.FromFamilyName(FontFamily, SKFontStyleWeight.SemiBold,
            SKFontStyleWidth.Normal, SKFontStyleSlant.Upright) ?? SKTypeface.Default;
    private static readonly SKTypeface BoldTypeface =
        SKTypeface.FromFamilyName(FontFamily, SKFontStyleWeight.Bold,
            SKFontStyleWidth.Normal, SKFontStyleSlant.Upright) ?? SKTypeface.Default;

    public record RenderResult(byte[] Pixels, Dictionary<string, SKRect> HitAreas);

    public RenderResult Render(DndMode currentDndMode, float opacity, double displayDurationSeconds, string statusText)
    {
        var hitAreas = new Dictionary<string, SKRect>();
        var info = new SKImageInfo(PanelWidth, PanelHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        DrawBackground(canvas);
        DrawHeader(canvas);
        DrawDndSection(canvas, currentDndMode, hitAreas);
        DrawOpacitySlider(canvas, opacity, hitAreas);
        DrawDurationSlider(canvas, displayDurationSeconds, hitAreas);
        DrawStatus(canvas, statusText);

        using var pixmap = surface.PeekPixels();
        var span = pixmap.GetPixelSpan();
        return new RenderResult(span.ToArray(), hitAreas);
    }

    private static void DrawBackground(SKCanvas canvas)
    {
        using var paint = new SKPaint
        {
            Color = BackgroundColor,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(0, 0, PanelWidth, PanelHeight), 16f), paint);
    }

    private static void DrawHeader(SKCanvas canvas)
    {
        using var paint = new SKPaint
        {
            Color = HeaderColor,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        // Header bar: top corners rounded via background, bottom corners square
        canvas.Save();
        canvas.ClipRoundRect(new SKRoundRect(new SKRect(0, 0, PanelWidth, PanelHeight), 16f));
        canvas.DrawRect(new SKRect(0, 0, PanelWidth, 64), paint);
        canvas.Restore();

        using var textPaint = CreateTextPaint(24f, SKFontStyleWeight.Bold);
        canvas.DrawText("VRNotify 設定", 32f, 42f, textPaint);
    }

    private static void DrawDndSection(SKCanvas canvas, DndMode currentMode, Dictionary<string, SKRect> hitAreas)
    {
        float y = 100f;

        using var labelPaint = CreateTextPaint(20f, SKFontStyleWeight.SemiBold);
        canvas.DrawText("おやすみモード", 32f, y + 24f, labelPaint);

        y += 50f;
        var buttons = new[]
        {
            ("dnd_off", "オフ", DndMode.Off),
            ("dnd_all", "すべて非表示", DndMode.SuppressAll),
            ("dnd_high", "重要な通知のみ", DndMode.HighPriorityOnly)
        };

        float btnX = 32f;
        const float btnWidth = 200f;
        const float btnHeight = 50f;
        const float btnGap = 20f;

        foreach (var (id, label, mode) in buttons)
        {
            var rect = new SKRect(btnX, y, btnX + btnWidth, y + btnHeight);
            var isActive = currentMode == mode;

            using var btnPaint = new SKPaint
            {
                Color = isActive ? ButtonActiveColor : ButtonColor,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            canvas.DrawRoundRect(new SKRoundRect(rect, 8f), btnPaint);

            using var btnTextPaint = CreateTextPaint(16f, isActive ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal);
            btnTextPaint.TextAlign = SKTextAlign.Center;
            canvas.DrawText(label, rect.MidX, rect.MidY + 6f, btnTextPaint);

            hitAreas[id] = rect;
            btnX += btnWidth + btnGap;
        }
    }

    private static void DrawOpacitySlider(SKCanvas canvas, float opacity, Dictionary<string, SKRect> hitAreas)
    {
        float y = 260f;
        const float sliderLeft = 32f;
        const float sliderRight = 700f;
        const float sliderHeight = 12f;
        const float handleRadius = 16f;

        using var labelPaint = CreateTextPaint(20f, SKFontStyleWeight.SemiBold);
        canvas.DrawText("不透明度", sliderLeft, y + 24f, labelPaint);

        using var valuePaint = CreateTextPaint(16f, SKFontStyleWeight.Normal);
        valuePaint.Color = SubTextColor;
        canvas.DrawText($"{opacity:P0}", sliderRight + 30f, y + 24f, valuePaint);

        y += 50f;
        // Track
        var trackRect = new SKRect(sliderLeft, y, sliderRight, y + sliderHeight);
        using var trackPaint = new SKPaint
        {
            Color = SliderTrackColor,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawRoundRect(new SKRoundRect(trackRect, sliderHeight / 2f), trackPaint);

        // Fill
        float normalizedOpacity = Math.Clamp((opacity - 0.1f) / 0.9f, 0f, 1f);
        float fillRight = sliderLeft + (sliderRight - sliderLeft) * normalizedOpacity;
        var fillRect = new SKRect(sliderLeft, y, fillRight, y + sliderHeight);
        using var fillPaint = new SKPaint
        {
            Color = SliderFillColor,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawRoundRect(new SKRoundRect(fillRect, sliderHeight / 2f), fillPaint);

        // Handle
        using var handlePaint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawCircle(fillRight, y + sliderHeight / 2f, handleRadius, handlePaint);

        // Hit area for the entire slider region
        hitAreas["opacity_slider"] = new SKRect(sliderLeft, y - handleRadius, sliderRight, y + sliderHeight + handleRadius);
    }

    private static void DrawDurationSlider(SKCanvas canvas, double durationSeconds, Dictionary<string, SKRect> hitAreas)
    {
        float y = 400f;
        const float sliderLeft = 32f;
        const float sliderRight = 700f;
        const float sliderHeight = 12f;
        const float handleRadius = 16f;
        const double minDuration = 2.0;
        const double maxDuration = 15.0;

        using var labelPaint = CreateTextPaint(20f, SKFontStyleWeight.SemiBold);
        canvas.DrawText("表示時間", sliderLeft, y + 24f, labelPaint);

        using var valuePaint = CreateTextPaint(16f, SKFontStyleWeight.Normal);
        valuePaint.Color = SubTextColor;
        canvas.DrawText($"{durationSeconds:F0}秒", sliderRight + 30f, y + 24f, valuePaint);

        y += 50f;
        // Track
        var trackRect = new SKRect(sliderLeft, y, sliderRight, y + sliderHeight);
        using var trackPaint = new SKPaint
        {
            Color = SliderTrackColor,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawRoundRect(new SKRoundRect(trackRect, sliderHeight / 2f), trackPaint);

        // Fill
        float normalizedDuration = (float)Math.Clamp((durationSeconds - minDuration) / (maxDuration - minDuration), 0.0, 1.0);
        float fillRight = sliderLeft + (sliderRight - sliderLeft) * normalizedDuration;
        var fillRect = new SKRect(sliderLeft, y, fillRight, y + sliderHeight);
        using var fillPaint = new SKPaint
        {
            Color = SliderFillColor,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawRoundRect(new SKRoundRect(fillRect, sliderHeight / 2f), fillPaint);

        // Handle
        using var handlePaint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawCircle(fillRight, y + sliderHeight / 2f, handleRadius, handlePaint);

        // Hit area
        hitAreas["duration_slider"] = new SKRect(sliderLeft, y - handleRadius, sliderRight, y + sliderHeight + handleRadius);
    }

    private static void DrawStatus(SKCanvas canvas, string statusText)
    {
        float y = 560f;

        using var labelPaint = CreateTextPaint(20f, SKFontStyleWeight.SemiBold);
        canvas.DrawText("ステータス", 32f, y + 24f, labelPaint);

        y += 50f;
        // Status indicator dot
        using var dotPaint = new SKPaint
        {
            Color = StatusGreenColor,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawCircle(44f, y + 8f, 6f, dotPaint);

        using var statusPaint = CreateTextPaint(16f, SKFontStyleWeight.Normal);
        statusPaint.Color = SubTextColor;
        canvas.DrawText(statusText, 60f, y + 14f, statusPaint);
    }

    private static SKPaint CreateTextPaint(float size, SKFontStyleWeight weight)
    {
        var typeface = weight switch
        {
            SKFontStyleWeight.Bold => BoldTypeface,
            SKFontStyleWeight.SemiBold => SemiBoldTypeface,
            _ => NormalTypeface
        };

        return new SKPaint
        {
            Color = TextColor,
            TextSize = size,
            IsAntialias = true,
            Typeface = typeface
        };
    }

    private static string ResolveFont()
    {
        using var test = SKTypeface.FromFamilyName("Yu Gothic UI");
        return test is not null && test.FamilyName == "Yu Gothic UI"
            ? "Yu Gothic UI"
            : "Segoe UI";
    }
}
