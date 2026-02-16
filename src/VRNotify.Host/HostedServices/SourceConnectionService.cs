using Microsoft.Extensions.Hosting;
using Serilog;
using VRNotify.Application.NotificationProcessing.Services;
using VRNotify.Domain.Configuration;
using VRNotify.Domain.NotificationProcessing;
using VRNotify.Domain.SourceConnection;

namespace VRNotify.Host.HostedServices;

public sealed class SourceConnectionService : BackgroundService
{
    private readonly ISourceAdapter _adapter;
    private readonly IFilterChain _filterChain;
    private readonly PriorityResolver _priorityResolver;
    private readonly INotificationHistory _history;
    private readonly INotificationQueue _queue;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILogger _logger;

    public SourceConnectionService(
        ISourceAdapter adapter,
        IFilterChain filterChain,
        PriorityResolver priorityResolver,
        INotificationHistory history,
        INotificationQueue queue,
        ISettingsRepository settingsRepository,
        ILogger logger)
    {
        _adapter = adapter;
        _filterChain = filterChain;
        _priorityResolver = priorityResolver;
        _history = history;
        _queue = queue;
        _settingsRepository = settingsRepository;
        _logger = logger.ForContext<SourceConnectionService>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _adapter.NotificationReceived += OnNotificationReceived;
        _adapter.ConnectionStateChanged += OnConnectionStateChanged;

        try
        {
            // WindowsNotificationAdapter does not use credentials
            await _adapter.ConnectAsync(new EncryptedCredential(Array.Empty<byte>(), Array.Empty<byte>()), stoppingToken);
            _logger.Information("Source connection service started");

            // Keep alive until cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Source connection service failed");
        }
        finally
        {
            _adapter.NotificationReceived -= OnNotificationReceived;
            _adapter.ConnectionStateChanged -= OnConnectionStateChanged;
            await _adapter.DisconnectAsync();
        }
    }

    private async Task OnNotificationReceived(NotificationEvent evt)
    {
        try
        {
            var settings = await _settingsRepository.LoadAsync();
            var profile = settings.GetActiveProfile();

            // Filter
            var filterResult = _filterChain.Evaluate(evt, profile.FilterRules);
            if (!filterResult.IsAllowed)
            {
                _logger.Debug("Notification filtered out: {AppName}", evt.Sender.Name);
                return;
            }

            // Priority
            var priority = filterResult.OverridePriority ?? _priorityResolver.Resolve(evt);

            // DND check
            if (profile.Dnd.Mode == DndMode.SuppressAll)
                return;
            if (profile.Dnd.Mode == DndMode.HighPriorityOnly && priority != Priority.High)
                return;

            // Duration
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
            await _history.SaveAsync(card);

            // Enqueue for display
            await _queue.EnqueueAsync(card);

            _logger.Debug("Notification enqueued: {Title} from {App}", card.Title, card.SenderDisplay);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing notification");
        }
    }

    private Task OnConnectionStateChanged(ConnectionState state)
    {
        _logger.Information("Source connection state changed to {State}", state);
        return Task.CompletedTask;
    }
}
