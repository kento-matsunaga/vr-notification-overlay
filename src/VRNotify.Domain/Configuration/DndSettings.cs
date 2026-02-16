namespace VRNotify.Domain.Configuration;

public sealed record DndSettings(
    DndMode Mode = DndMode.Off);
