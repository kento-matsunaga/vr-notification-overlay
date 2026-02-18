using System.Drawing;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using VRNotify.Application.Configuration.Services;
using VRNotify.Desktop.ViewModels;
using VRNotify.Desktop.Views;
using VRNotify.Domain.Configuration;
using VRNotify.Host.DependencyInjection;
using WpfApplication = System.Windows.Application;

namespace VRNotify.Desktop;

public partial class App : WpfApplication
{
    private IHost? _host;
    private TaskbarIcon? _trayIcon;
    private SettingsWindow? _settingsWindow;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Configure Serilog
        var logPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VRNotify", "logs", "vrnotify-.log");
        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
            .CreateLogger();

        Log.Information("VRNotify starting up");

        // Check Package Identity (required for Windows notification listener)
        if (!HasPackageIdentity())
        {
            Log.Warning("No Package Identity detected");
            MessageBox.Show(
                "VRNotify はスタートメニューから起動するか、\n" +
                "SteamVR の自動起動をご利用ください。\n\n" +
                "EXE を直接起動すると Windows 通知の受信に\n" +
                "必要な権限が付与されません。",
                "VRNotify - 起動エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            Shutdown();
            return;
        }

        // Build host with DI
        _host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices(services =>
            {
                services.AddVRNotifyServices();

                // ViewModels
                services.AddTransient<FilterViewModel>();
                services.AddTransient<DisplayViewModel>();
                services.AddTransient<HistoryViewModel>();
                services.AddTransient<MainViewModel>();
            })
            .Build();

        // Setup system tray
        SetupTrayIcon();

        // Start hosted services
        await _host.StartAsync();
        Log.Information("VRNotify started successfully");
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        Log.Information("VRNotify shutting down");

        _settingsWindow?.Close();
        _trayIcon?.Dispose();

        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new TaskbarIcon
        {
            Icon = CreateDefaultIcon(),
            ToolTipText = "VRNotify - VR Notification Overlay",
            ContextMenu = CreateTrayMenu()
        };
        _trayIcon.TrayMouseDoubleClick += (_, _) => OpenSettings();
    }

    private System.Windows.Controls.ContextMenu CreateTrayMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();

        var settingsItem = new System.Windows.Controls.MenuItem { Header = "設定..." };
        settingsItem.Click += (_, _) => OpenSettings();
        menu.Items.Add(settingsItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var dndMenu = new System.Windows.Controls.MenuItem { Header = "おやすみモード" };
        var dndOff = new System.Windows.Controls.MenuItem { Header = "オフ" };
        dndOff.Click += (_, _) => SetDndMode(DndMode.Off);
        var dndAll = new System.Windows.Controls.MenuItem { Header = "すべて非表示" };
        dndAll.Click += (_, _) => SetDndMode(DndMode.SuppressAll);
        var dndHigh = new System.Windows.Controls.MenuItem { Header = "重要な通知のみ" };
        dndHigh.Click += (_, _) => SetDndMode(DndMode.HighPriorityOnly);
        dndMenu.Items.Add(dndOff);
        dndMenu.Items.Add(dndAll);
        dndMenu.Items.Add(dndHigh);
        menu.Items.Add(dndMenu);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var exitItem = new System.Windows.Controls.MenuItem { Header = "終了" };
        exitItem.Click += (_, _) => Shutdown();
        menu.Items.Add(exitItem);

        return menu;
    }

    private void OpenSettings()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        var viewModel = _host!.Services.GetRequiredService<MainViewModel>();
        _settingsWindow = new SettingsWindow(viewModel);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    private async void SetDndMode(DndMode mode)
    {
        try
        {
            var settingsService = _host?.Services.GetService<ISettingsService>();
            if (settingsService is null) return;

            await settingsService.ToggleDndAsync(mode);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to change DND mode");
        }
    }

    private static bool HasPackageIdentity()
    {
        try
        {
            var package = global::Windows.ApplicationModel.Package.Current;
            return package is not null;
        }
        catch
        {
            return false;
        }
    }

    private static Icon CreateDefaultIcon()
    {
        // Generate a simple 16x16 icon with "VR" text
        using var bitmap = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bitmap);
        g.Clear(Color.FromArgb(88, 101, 242)); // Discord-like blue
        using var font = new Font("Segoe UI", 7f, System.Drawing.FontStyle.Bold);
        using var brush = new SolidBrush(Color.White);
        g.DrawString("VR", font, brush, 0, 2);
        var hIcon = bitmap.GetHicon();
        return Icon.FromHandle(hIcon);
    }
}
