using Microsoft.Extensions.Hosting;
using Serilog;
using VRNotify.Domain.VRDisplay;

namespace VRNotify.Host.HostedServices;

public sealed class OpenVrHostedService : BackgroundService
{
    private readonly IOverlayManager _overlayManager;
    private readonly ILogger _logger;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    public OpenVrHostedService(IOverlayManager overlayManager, ILogger logger)
    {
        _overlayManager = overlayManager;
        _logger = logger.ForContext<OpenVrHostedService>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Information("OpenVR hosted service started, waiting for SteamVR...");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!_overlayManager.IsAvailable)
            {
                try
                {
                    await _overlayManager.InitializeAsync(stoppingToken);
                    _logger.Information("SteamVR connected, overlay initialized");
                }
                catch (Exception ex)
                {
                    _logger.Debug("SteamVR not available: {Message}", ex.Message);
                }
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        await _overlayManager.DisposeAsync();
        _logger.Information("OpenVR hosted service stopped");
    }
}
