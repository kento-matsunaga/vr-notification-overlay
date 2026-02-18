namespace VRNotify.Domain.VRDisplay;

public interface IDashboardOverlay : IAsyncDisposable
{
    bool IsAvailable { get; }
    Task InitializeAsync(CancellationToken ct = default);
    event Action<string>? ButtonClicked;
}
