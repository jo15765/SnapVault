using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Interactivity;
using SnapVault.Models;
using SnapVault.Services;

namespace SnapVault.Views;

public partial class RestoreStep2SelectBackup : UserControl
{
    private readonly BackupWizardState _state;

    public RestoreStep2SelectBackup(BackupWizardState state)
    {
        InitializeComponent();
        _state = state;
    }

    private async void RefreshButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_state.CloudConfig == null || !IsValidConfig())
        {
            StatusText.Text = "Enter cloud credentials in step 1.";
            return;
        }
        RefreshButton.IsEnabled = false;
        StatusText.Text = "Loading...";
        try
        {
            var (success, sets, message) = await CloudStorageService.ListBackupSetsAsync(_state.CloudConfig);
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (success)
                    this.FindControl<ListBox>("BackupSetsList")!.ItemsSource = sets;
                this.FindControl<TextBlock>("StatusText")!.Text = success ? (sets.Count == 0 ? "No backup sets found." : $"{sets.Count} backup(s) found.") : message;
                RefreshButton.IsEnabled = true;
            });
        }
        finally
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { RefreshButton.IsEnabled = true; });
        }
    }

    private void BackupSetsList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (this.FindControl<ListBox>("BackupSetsList")?.SelectedItem is CloudBackupSetInfo set)
            _state.RestoreSelectedBackupSet = set;
    }

    private bool IsValidConfig() =>
        _state.CloudConfig != null
        && !string.IsNullOrWhiteSpace(_state.CloudConfig.ContainerOrBucket)
        && !string.IsNullOrWhiteSpace(_state.CloudConfig.AccountNameOrKeyId)
        && !string.IsNullOrWhiteSpace(_state.CloudConfig.SecretKey);

    public bool IsValid() => _state.RestoreSelectedBackupSet != null;
}
