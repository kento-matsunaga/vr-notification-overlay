using SkiaSharp;
using VRNotify.Domain.NotificationProcessing;
using VRNotify.Domain.SourceConnection;
using VRNotify.Domain.VRDisplay;

namespace VRNotify.Overlay.Rendering;

public sealed class SkiaNotificationRenderer : IOverlayRenderer
{
    private const float CornerRadius = 12f;
    private const float BorderWidth = 4f;
    private const float PaddingLeft = 16f;
    private const float PaddingTop = 12f;

    private static readonly SKColor BackgroundColor = new(0x1a, 0x1a, 0x2e, 0xCC);
    private static readonly SKColor DiscordColor = new(0x58, 0x65, 0xF2);
    private static readonly SKColor SlackColor = new(0x61, 0x1F, 0x69);
    private static readonly SKColor SenderTextColor = SKColors.White;
    private static readonly SKColor BodyTextColor = new(0xCC, 0xCC, 0xCC);
    private static readonly SKColor HighPriorityColor = new(0xFF, 0x44, 0x44);
    private static readonly SKColor MediumPriorityColor = new(0xFF, 0xCC, 0x00);

    public byte[] RenderCard(NotificationCard card, int width, int height)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        DrawBackground(canvas, width, height);
        DrawServiceBorder(canvas, card.SourceType, height);
        DrawPriorityIndicator(canvas, card.Priority, width);
        DrawText(canvas, card);

        using var pixmap = surface.PeekPixels();
        var span = pixmap.GetPixelSpan();
        return span.ToArray();
    }

    private static void DrawBackground(SKCanvas canvas, int width, int height)
    {
        using var paint = new SKPaint
        {
            Color = BackgroundColor,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        var rect = new SKRoundRect(new SKRect(0, 0, width, height), CornerRadius);
        canvas.DrawRoundRect(rect, paint);
    }

    private static void DrawServiceBorder(SKCanvas canvas, SourceType sourceType, int height)
    {
        var color = sourceType switch
        {
            SourceType.Discord => DiscordColor,
            SourceType.Slack => SlackColor,
            _ => DiscordColor
        };

        using var paint = new SKPaint
        {
            Color = color,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        // Left border with rounded corners on the left side
        var path = new SKPath();
        path.AddRoundRect(new SKRect(0, 0, BorderWidth + CornerRadius, height),
            CornerRadius, CornerRadius);
        // Clip to only show the left portion
        canvas.Save();
        canvas.ClipRect(new SKRect(0, 0, BorderWidth, height));
        canvas.DrawPath(path, paint);
        canvas.Restore();
        path.Dispose();
    }

    private static void DrawPriorityIndicator(SKCanvas canvas, Priority priority, int width)
    {
        if (priority == Priority.Low) return;

        var color = priority == Priority.High ? HighPriorityColor : MediumPriorityColor;
        using var paint = new SKPaint
        {
            Color = color,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawCircle(width - 16f, 16f, 5f, paint);
    }

    private static void DrawText(SKCanvas canvas, NotificationCard card)
    {
        var typeface = SKTypeface.FromFamilyName("Segoe UI") ?? SKTypeface.Default;

        // Sender name
        using var senderPaint = new SKPaint
        {
            Color = SenderTextColor,
            TextSize = 16f,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyleWeight.Bold,
                SKFontStyleWidth.Normal, SKFontStyleSlant.Upright) ?? typeface
        };
        var senderX = BorderWidth + PaddingLeft;
        var senderY = PaddingTop + senderPaint.TextSize;
        canvas.DrawText(card.SenderDisplay, senderX, senderY, senderPaint);

        // Channel / title (small, gray)
        using var titlePaint = new SKPaint
        {
            Color = new SKColor(0x88, 0x88, 0x88),
            TextSize = 12f,
            IsAntialias = true,
            Typeface = typeface
        };
        var titleX = senderX + senderPaint.MeasureText(card.SenderDisplay) + 8f;
        canvas.DrawText(card.Title, titleX, senderY, titlePaint);

        // Body text
        using var bodyPaint = new SKPaint
        {
            Color = BodyTextColor,
            TextSize = 14f,
            IsAntialias = true,
            Typeface = typeface
        };
        var bodyY = senderY + 24f;

        // Truncate body if too long for the card width
        var maxWidth = 512f - senderX - 20f;
        var bodyText = card.Body;
        if (bodyPaint.MeasureText(bodyText) > maxWidth)
        {
            while (bodyText.Length > 0 && bodyPaint.MeasureText(bodyText + "...") > maxWidth)
                bodyText = bodyText[..^1];
            bodyText += "...";
        }
        canvas.DrawText(bodyText, senderX, bodyY, bodyPaint);

        typeface.Dispose();
    }
}
