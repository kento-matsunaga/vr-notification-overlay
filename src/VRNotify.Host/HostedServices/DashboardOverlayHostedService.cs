using Microsoft.Extensions.Hosting;
using Serilog;
using VRNotify.Application.Configuration.Services;
using VRNotify.Domain.Configuration;
using VRNotify.Domain.VRDisplay;
using VRNotify.Overlay.OpenVR;

namespace VRNotify.Host.HostedServices;

public sealed class DashboardOverlayHostedService : BackgroundService
{
    private readonly OpenVrDashboardOverlay _dashboardOverlay;
    private readonly ISettingsService _settingsService;
    private readonly IOverlayManager _overlayManager;
    private readonly ILogger _logger;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(5);

    public DashboardOverlayHostedService(
        IDashboardOverlay dashboardOverlay,
        ISettingsService settingsService,
        IOverlayManager overlayManager,
        ILogger logger)
    {
        _dashboardOverlay = (OpenVrDashboardOverlay)dashboardOverlay;
        _settingsService = settingsService;
        _overlayManager = overlayManager;
        _logger = logger.ForContext<DashboardOverlayHostedService>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Information("Dashboard overlay service started, waiting for SteamVR...");

        // Wait for the main overlay to be available (SteamVR connected)
        while (!stoppingToken.IsCancellationRequested && !_overlayManager.IsAvailable)
        {
            try
            {
                await Task.Delay(RetryInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        // Initialize dashboard overlay
        if (!_dashboardOverlay.IsAvailable)
        {
            try
            {
                await _dashboardOverlay.InitializeAsync(stoppingToken);
                _dashboardOverlay.ButtonClicked += OnButtonClicked;
                await RenderCurrentSettings();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to initialize dashboard overlay");
                return;
            }
        }

        // Event polling loop
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _dashboardOverlay.PollEvents();
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Dashboard overlay polling error");
                await Task.Delay(RetryInterval, stoppingToken);
            }
        }

        _dashboardOverlay.ButtonClicked -= OnButtonClicked;
        await _dashboardOverlay.DisposeAsync();
        _logger.Information("Dashboard overlay service stopped");
    }

    private async void OnButtonClicked(string buttonId)
    {
        try
        {
            var settings = await _settingsService.LoadAsync();
            var profile = settings.GetActiveProfile();

            if (buttonId == "dnd_off")
            {
                await _settingsService.ToggleDndAsync(DndMode.Off);
            }
            else if (buttonId == "dnd_all")
            {
                await _settingsService.ToggleDndAsync(DndMode.SuppressAll);
            }
            else if (buttonId == "dnd_high")
            {
                await _settingsService.ToggleDndAsync(DndMode.HighPriorityOnly);
            }
            else if (buttonId.StartsWith("opacity:"))
            {
                var value = float.Parse(buttonId.Split(':')[1], System.Globalization.CultureInfo.InvariantCulture);
                value = Math.Clamp(value, 0.1f, 1.0f);
                var newDisplay = profile.Display with { Opacity = value };
                profile.UpdateDisplay(newDisplay);
                await _settingsService.SaveAsync(settings);
            }
            else if (buttonId.StartsWith("duration:"))
            {
                var seconds = double.Parse(buttonId.Split(':')[1], System.Globalization.CultureInfo.InvariantCulture);
                seconds = Math.Clamp(seconds, 2.0, 15.0);
                var baseDuration = TimeSpan.FromSeconds(seconds);
                var newDisplay = DisplayConfig.WithBaseDuration(
                    baseDuration,
                    position: profile.Display.Position,
                    slotCount: profile.Display.SlotCount,
                    opacity: profile.Display.Opacity,
                    scale: profile.Display.Scale);
                profile.UpdateDisplay(newDisplay);
                await _settingsService.SaveAsync(settings);
            }

            await RenderCurrentSettings();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to handle dashboard button: {ButtonId}", buttonId);
        }
    }

    private async Task RenderCurrentSettings()
    {
        var settings = await _settingsService.LoadAsync();
        var profile = settings.GetActiveProfile();

        _dashboardOverlay.RenderPanel(
            profile.Dnd.Mode,
            profile.Display.Opacity,
            profile.Display.GetLowPriorityDuration().TotalSeconds,
            "接続中 - 通知リスナー稼働中");
    }
}
