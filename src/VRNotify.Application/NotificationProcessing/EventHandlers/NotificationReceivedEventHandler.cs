using MediatR;
using VRNotify.Application.Common;
using VRNotify.Application.NotificationProcessing.Services;
using VRNotify.Domain.Configuration;
using VRNotify.Domain.NotificationProcessing;
using VRNotify.Domain.SourceConnection.Events;

namespace VRNotify.Application.NotificationProcessing.EventHandlers;

public sealed class NotificationReceivedEventHandler
    : INotificationHandler<DomainEventNotification<NotificationReceivedEvent>>
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IFilterChain _filterChain;
    private readonly PriorityResolver _priorityResolver;
    private readonly INotificationHistory _history;
    private readonly INotificationQueue _queue;

    public NotificationReceivedEventHandler(
        ISettingsRepository settingsRepository,
        IFilterChain filterChain,
        PriorityResolver priorityResolver,
        INotificationHistory history,
        INotificationQueue queue)
    {
        _settingsRepository = settingsRepository;
        _filterChain = filterChain;
        _priorityResolver = priorityResolver;
        _history = history;
        _queue = queue;
    }

    public async Task Handle(
        DomainEventNotification<NotificationReceivedEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.DomainEvent.Notification;
        var settings = await _settingsRepository.LoadAsync(cancellationToken);
        var profile = settings.GetActiveProfile();

        // Filter
        var filterResult = _filterChain.Evaluate(evt, profile.FilterRules);
        if (!filterResult.IsAllowed)
            return;

        // Priority
        var priority = filterResult.OverridePriority ?? _priorityResolver.Resolve(evt);

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
            evt.EventId,
            evt.SourceType,
            priority,
            evt.Channel.Name,
            evt.Content.TruncatedText,
            evt.Sender.Name,
            evt.Sender.AvatarUrl,
            duration);

        // Save history
        await _history.SaveAsync(card, cancellationToken);

        // Enqueue for display
        await _queue.EnqueueAsync(card, cancellationToken);
    }
}
