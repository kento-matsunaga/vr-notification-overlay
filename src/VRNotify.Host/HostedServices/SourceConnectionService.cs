using Microsoft.Extensions.Hosting;
using Serilog;
using VRNotify.Application.NotificationProcessing.Services;
using VRNotify.Domain.NotificationProcessing;
using VRNotify.Domain.SourceConnection;

namespace VRNotify.Host.HostedServices;

public sealed class SourceConnectionService : BackgroundService
{
    private readonly ISourceAdapter _adapter;
    private readonly INotificationPipelineService _pipeline;
    private readonly ILogger _logger;

    public SourceConnectionService(
        ISourceAdapter adapter,
        INotificationPipelineService pipeline,
        ILogger logger)
    {
        _adapter = adapter;
        _pipeline = pipeline;
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
            await _pipeline.ProcessAsync(evt);
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
