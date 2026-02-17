using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VRNotify.Application.Configuration.Services;
using VRNotify.Domain.Configuration;

namespace VRNotify.Desktop.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private FilterViewModel _filter;

    [ObservableProperty]
    private DisplayViewModel _display;

    [ObservableProperty]
    private HistoryViewModel _history;

    [ObservableProperty]
    private DndMode _currentDndMode;

    [ObservableProperty]
    private bool _isLoading;

    public MainViewModel(
        ISettingsService settingsService,
        FilterViewModel filter,
        DisplayViewModel display,
        HistoryViewModel history)
    {
        _settingsService = settingsService;
        _filter = filter;
        _display = display;
        _history = history;
    }

    [RelayCommand]
    private async Task LoadSettingsAsync()
    {
        IsLoading = true;
        try
        {
            var settings = await _settingsService.LoadAsync();
            var profile = settings.GetActiveProfile();
            CurrentDndMode = profile.Dnd.Mode;
            Filter.LoadFromProfile(profile);
            Display.LoadFromProfile(profile);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SetDndModeAsync(DndMode mode)
    {
        await _settingsService.ToggleDndAsync(mode);
        CurrentDndMode = mode;
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        var settings = await _settingsService.LoadAsync();
        var profile = settings.GetActiveProfile();
        Display.ApplyToProfile(profile);
        Filter.ApplyToProfile(profile);
        await _settingsService.SaveAsync(settings);
    }
}
