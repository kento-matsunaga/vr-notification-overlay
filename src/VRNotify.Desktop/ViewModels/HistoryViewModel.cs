using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VRNotify.Application.Configuration.Services;
using VRNotify.Domain.NotificationProcessing;

namespace VRNotify.Desktop.ViewModels;

public sealed partial class HistoryViewModel : ObservableObject
{
    private readonly INotificationHistory _history;
    private readonly ISettingsService _settingsService;

    public ObservableCollection<HistoryEntry> Entries { get; } = new();

    [ObservableProperty]
    private int _retentionDays = 7;

    [ObservableProperty]
    private int _maxEntries = 1000;

    public HistoryViewModel(INotificationHistory history, ISettingsService settingsService)
    {
        _history = history;
        _settingsService = settingsService;
    }

    [RelayCommand]
    private async Task LoadHistoryAsync()
    {
        var settings = await _settingsService.LoadAsync();
        RetentionDays = settings.History.RetentionDays;
        MaxEntries = settings.History.MaxEntries;

        var cards = await _history.GetRecentAsync(100);
        Entries.Clear();
        foreach (var card in cards)
        {
            Entries.Add(new HistoryEntry
            {
                Time = card.CreatedAt.LocalDateTime,
                AppName = card.SenderDisplay,
                Sender = card.Title,
                Message = card.Body
            });
        }
    }

    [RelayCommand]
    private async Task ClearHistoryAsync()
    {
        await _history.PurgeOldEntriesAsync(TimeSpan.Zero, 0);
        Entries.Clear();
    }
}

public sealed class HistoryEntry
{
    public DateTime Time { get; set; }
    public string AppName { get; set; } = "";
    public string Sender { get; set; } = "";
    public string Message { get; set; } = "";

    public string TimeDisplay => Time.ToString("HH:mm");
}
