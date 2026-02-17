using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Interactivity;
using SnapVault.Models;
using SnapVault.Services;

namespace SnapVault.Views;

public partial class Step2SelectDestination : UserControl
{
    private readonly BackupWizardState _state;

    public Step2SelectDestination(BackupWizardState state)
    {
        InitializeComponent();
        _state = state;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        var drives = DriveService.GetDrives().ToList();
        if (_state.SourceDrive != null)
        {
            var sourceName = _state.SourceDrive.Name;
            drives = drives.Where(d => !string.Equals(d.Name, sourceName, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        this.FindControl<ListBox>("DestinationList")!.ItemsSource = drives;
        UpdateSizeEstimate();
        NotifyValidityChanged();
    }

    private void DestinationList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (this.FindControl<ListBox>("DestinationList")?.SelectedItem is DriveInfoModel d)
            _state.DestinationDrive = d;
        UpdateSizeEstimate();
        NotifyValidityChanged();
    }

    private void UpdateSizeEstimate()
    {
        var sizeLabel = this.FindControl<TextBlock>("SizeEstimateLabel");
        var warningText = this.FindControl<TextBlock>("SpaceWarningText");
        if (_state.SourceDrive == null)
        {
            sizeLabel!.Text = "Estimated full backup size: —";
            if (warningText != null) warningText.IsVisible = false;
            return;
        }
        _state.EstimatedBackupSizeBytes = DriveService.EstimateBackupSize(_state.SourceDrive);
        sizeLabel!.Text = $"Estimated full backup size: {DriveInfoModel.FormatBytes(_state.EstimatedBackupSizeBytes)}. Continue?";
        if (warningText != null)
        {
            var insufficient = _state.DestinationDrive != null && _state.DestinationDrive.FreeBytes < _state.EstimatedBackupSizeBytes;
            warningText.IsVisible = insufficient;
        }
    }

    private void NotifyValidityChanged()
    {
        if (VisualRoot is SnapVault.MainWindow w)
            w.UpdateNextButtonState();
    }

    public bool IsValid()
    {
        if (_state.DestinationDrive == null) return false;
        return _state.DestinationDrive.FreeBytes >= _state.EstimatedBackupSizeBytes;
    }
}
