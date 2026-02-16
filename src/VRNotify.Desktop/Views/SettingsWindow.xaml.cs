using System.Windows;
using VRNotify.Desktop.ViewModels;

namespace VRNotify.Desktop.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) =>
        {
            await viewModel.LoadSettingsCommand.ExecuteAsync(null);
            await viewModel.History.LoadHistoryCommand.ExecuteAsync(null);
        };
        Closing += async (_, e) =>
        {
            e.Cancel = true;
            await viewModel.SaveSettingsCommand.ExecuteAsync(null);
            Hide();
        };
    }
}
