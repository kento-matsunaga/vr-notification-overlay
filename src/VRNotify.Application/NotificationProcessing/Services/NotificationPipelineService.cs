using Serilog;
using VRNotify.Domain.Configuration;
using VRNotify.Domain.NotificationProcessing;

namespace VRNotify.Application.NotificationProcessing.Services;

public sealed class NotificationPipelineService : INotificationPipelineService
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IFilterChain _filterChain;
    private readonly IPriorityResolver _priorityResolver;
    private readonly INotificationHistory _history;
    private readonly INotificationQueue _queue;
    private readonly ILogger _logger;

    public NotificationPipelineService(
        ISettingsRepository settingsRepository,
        IFilterChain filterChain,
        IPriorityResolver priorityResolver,
        INotificationHistory history,
        INotificationQueue queue,
        ILogger logger)
    {
        _settingsRepository = settingsRepository;
        _filterChain = filterChain;
        _priorityResolver = priorityResolver;
        _history = history;
        _queue = queue;
        _logger = logger.ForContext<NotificationPipelineService>();
    }

    public async Task ProcessAsync(NotificationEvent notification, CancellationToken ct = default)
    {
        var settings = await _settingsRepository.LoadAsync(ct);
        var profile = settings.GetActiveProfile();

        // Filter
        var filterResult = _filterChain.Evaluate(notification, profile.FilterRules);
        if (!filterResult.IsAllowed)
        {
            _logger.Debug("Notification filtered out: {AppName}", notification.Sender.Name);
            return;
        }

        // Priority
        var priority = filterResult.OverridePriority ?? _priorityResolver.Resolve(notification);

        // DND check
        if (profile.Dnd.Mode == DndMode.SuppressAll)
            return;
        if (profile.Dnd.Mode == DndMode.HighPriorityOnly && priority != Priority.High)
            return;

        // Duration based on priority
        var duration = priority switch
        {
            Priority.High => profile.Display.GetHighPriorityDuration(),
            Priority.Medium => profile.Display.GetMediumPriorityDuration(),
            _ => profile.Display.GetLowPriorityDuration()
        };

        // Create card
        var card = new NotificationCard(
            Guid.NewGuid(),
            notification.EventId,
            notification.SourceType,
            priority,
            notification.Channel.Name,
            notification.Content.TruncatedText,
            notification.Sender.Name,
            notification.Sender.AvatarUrl,
            duration);

        // Save history
        await _history.SaveAsync(card, ct);

        // Enqueue for display
        await _queue.EnqueueAsync(card, ct);

        _logger.Debug("Notification enqueued: {Title} from {App}", card.Title, card.SenderDisplay);
    }
}
