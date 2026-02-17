using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SnapVault.Models;
using SnapVault.Services;

namespace SnapVault.Views;

public partial class EditScheduleDialog : Window
{
    private readonly BackupWizardState _state;
    private readonly IBackupEngine _engine = BackupEngineFactory.Instance;

    public EditScheduleDialog(BackupWizardState state)
    {
        AvaloniaXamlLoader.Load(this);
        _state = state;
        var fullDays = this.FindControl<ComboBox>("FullBackupDays");
        var incInterval = this.FindControl<ComboBox>("IncrementalInterval");
        if (fullDays != null)
            fullDays.SelectedIndex = _state.FullBackupIntervalDays switch { 1 => 0, 3 => 1, 7 => 2, 14 => 3, 30 => 4, _ => 2 };
        if (incInterval != null)
            incInterval.SelectedIndex = _state.IncrementalIntervalHours switch { 0 => 0, 12 => 1, 24 => 2, 36 => 3, 48 => 4, _ => 2 };
    }

    private void Cancel_OnClick(object? sender, RoutedEventArgs e) => Close();

    private async void Apply_OnClick(object? sender, RoutedEventArgs e)
    {
        var fullDays = this.FindControl<ComboBox>("FullBackupDays");
        var incInterval = this.FindControl<ComboBox>("IncrementalInterval");
        if (fullDays != null)
            _state.FullBackupIntervalDays = fullDays.SelectedIndex switch { 0 => 1, 1 => 3, 2 => 7, 3 => 14, 4 => 30, _ => 7 };
        if (incInterval != null)
            _state.IncrementalIntervalHours = incInterval.SelectedIndex switch { 0 => 0, 1 => 12, 2 => 24, 3 => 36, 4 => 48, _ => 24 };

        BackupConfigService.Save(_state);
        var target = _state.DestinationDrive?.Name?.TrimEnd('\\', '/') ?? "";
        var (ok, msg) = _engine.ScheduleBackups(_state, target);
        if (ok)
            await DialogHelper.ShowAsync("Schedule", msg);
        else
            await DialogHelper.ShowWarningAsync("Schedule", msg);
        Close();
    }
}
