using VRNotify.Domain.NotificationProcessing;

namespace VRNotify.Application.NotificationProcessing.Services;

/// <summary>
/// Processes incoming notifications through the full pipeline:
/// filter → priority → DND → card creation → history → queue.
/// </summary>
public interface INotificationPipelineService
{
    Task ProcessAsync(NotificationEvent notification, CancellationToken ct = default);
}
