using Avalonia.Controls;
using Avalonia.Controls.Selection;
using SnapVault.Models;
using SnapVault.Services;

namespace SnapVault.Views;

public partial class Step1SelectSource : UserControl
{
    private readonly BackupWizardState _state;

    public Step1SelectSource(BackupWizardState state)
    {
        InitializeComponent();
        _state = state;
        Loaded += (_, _) =>
        {
            var list = this.FindControl<ListBox>("DrivesList");
            if (list != null) list.ItemsSource = DriveService.GetDrives();
            NotifyValidityChanged();
        };
    }

    private void DrivesList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (this.FindControl<ListBox>("DrivesList")?.SelectedItem is DriveInfoModel d)
            _state.SourceDrive = d;
        NotifyValidityChanged();
    }

    private void NotifyValidityChanged()
    {
        if (VisualRoot is SnapVault.MainWindow w)
            w.UpdateNextButtonState();
    }

    public bool IsValid() => _state.SourceDrive != null;
}
