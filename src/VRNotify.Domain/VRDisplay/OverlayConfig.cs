namespace VRNotify.Domain.VRDisplay;

public sealed record OverlayConfig(
    float WidthInMeters = 0.3f,
    float HeightInMeters = 0.08f,
    float DistanceFromHmd = 1.2f,
    float AngleFromCenter = 15f,
    float Opacity = 1.0f,
    float Scale = 1.0f);
