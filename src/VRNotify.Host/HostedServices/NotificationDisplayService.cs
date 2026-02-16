using Microsoft.Extensions.Hosting;

namespace VRNotify.Host.HostedServices;

public sealed class NotificationDisplayService : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => throw new NotImplementedException();
}
