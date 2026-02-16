namespace VRNotify.Domain.SourceConnection;

public sealed record ReconnectPolicy(
    TimeSpan InitialDelay,
    TimeSpan MaxDelay,
    int MaxAttempts,
    double JitterFactor)
{
    public static ReconnectPolicy Default => new(
        InitialDelay: TimeSpan.FromSeconds(1),
        MaxDelay: TimeSpan.FromSeconds(60),
        MaxAttempts: 10,
        JitterFactor: 0.2);
}
