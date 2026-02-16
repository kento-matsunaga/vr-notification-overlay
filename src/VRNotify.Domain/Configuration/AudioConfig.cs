namespace VRNotify.Domain.Configuration;

public sealed record AudioConfig(
    bool IsEnabled = true,
    float Volume = 0.3f);
