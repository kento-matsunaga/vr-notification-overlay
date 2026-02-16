using Microsoft.Extensions.Hosting;
using Serilog;
using VRNotify.Application.VRDisplay.Services;
using VRNotify.Domain.Configuration;
using VRNotify.Domain.NotificationProcessing;
using VRNotify.Domain.VRDisplay;

namespace VRNotify.Host.HostedServices;

public sealed class NotificationDisplayService : BackgroundService
{
    private readonly INotificationQueue _queue;
    private readonly IOverlayManager _overlayManager;
    private readonly DisplaySlotManager _slotManager;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILogger _logger;

    public NotificationDisplayService(
        INotificationQueue queue,
        IOverlayManager overlayManager,
        DisplaySlotManager slotManager,
        ISettingsRepository settingsRepository,
        ILogger logger)
    {
        _queue = queue;
        _overlayManager = overlayManager;
        _slotManager = slotManager;
        _settingsRepository = settingsRepository;
        _logger = logger.ForContext<NotificationDisplayService>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Information("Notification display service started");

        await foreach (var card in _queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                if (!_overlayManager.IsAvailable)
                {
                    _logger.Debug("Overlay not available, skipping notification: {Title}", card.Title);
                    continue;
                }

                var slot = _slotManager.FindAvailableSlot();
                if (slot is null)
                {
                    _logger.Debug("No available display slot, skipping notification: {Title}", card.Title);
                    continue;
                }

                await _overlayManager.ShowNotificationAsync(card, slot, stoppingToken);
                _logger.Debug("Displaying notification: {Title} for {Duration}s", card.Title, card.DisplayDuration.TotalSeconds);

                // Schedule hide after display duration
                _ = HideAfterDurationAsync(slot, card.DisplayDuration, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error displaying notification: {Title}", card.Title);
            }
        }
    }

    private async Task HideAfterDurationAsync(DisplaySlot slot, TimeSpan duration, CancellationToken ct)
    {
        try
        {
            await Task.Delay(duration, ct);
            await _overlayManager.HideSlotAsync(slot, ct);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error hiding notification slot {SlotIndex}", slot.SlotIndex);
        }
    }
}
