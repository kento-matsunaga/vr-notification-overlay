namespace VRNotify.Domain.Configuration;

public sealed record HistoryConfig(
    int RetentionDays = 7,
    int MaxEntries = 1000);
