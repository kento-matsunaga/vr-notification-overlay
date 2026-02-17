using Microsoft.Extensions.DependencyInjection;
using Serilog;
using VRNotify.Application.Configuration.Services;
using VRNotify.Application.NotificationProcessing.Services;
using VRNotify.Application.VRDisplay.Services;
using VRNotify.Domain.Configuration;
using VRNotify.Domain.NotificationProcessing;
using VRNotify.Domain.SourceConnection;
using VRNotify.Domain.VRDisplay;
using VRNotify.Host.HostedServices;
using VRNotify.Infrastructure.Filtering;
using VRNotify.Infrastructure.Persistence;
using VRNotify.Infrastructure.Queuing;
using VRNotify.Infrastructure.Windows;
using VRNotify.Overlay.OpenVR;

namespace VRNotify.Host.DependencyInjection;

public static class ServiceRegistration
{
    public static IServiceCollection AddVRNotifyServices(this IServiceCollection services)
    {
        // Serilog
        services.AddSingleton(Log.Logger);

        // Infrastructure - Persistence
        services.AddSingleton<ISettingsRepository, JsonSettingsRepository>();
        services.AddSingleton<INotificationHistory, SqliteNotificationHistory>();
        services.AddSingleton<INotificationQueue, ChannelNotificationQueue>();

        // Infrastructure - Filtering
        services.AddSingleton<IFilterChain, DefaultFilterChain>();

        // Infrastructure - Windows
        services.AddSingleton<ISourceAdapter, WindowsNotificationAdapter>();

        // Application - Services
        services.AddSingleton<IPriorityResolver, PriorityResolver>();
        services.AddSingleton<IDisplaySlotManager, DisplaySlotManager>();
        services.AddSingleton<INotificationPipelineService, NotificationPipelineService>();
        services.AddSingleton<ISettingsService, SettingsService>();

        // Overlay
        services.AddSingleton<IOverlayManager, OpenVrOverlayManager>();

        // HostedServices
        services.AddHostedService<OpenVrHostedService>();
        services.AddHostedService<SourceConnectionService>();
        services.AddHostedService<NotificationDisplayService>();

        return services;
    }
}
