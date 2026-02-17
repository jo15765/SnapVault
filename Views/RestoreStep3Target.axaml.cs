using System;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Interactivity;
using SnapVault.Models;
using SnapVault.Services;

namespace SnapVault.Views;

public partial class RestoreStep3Target : UserControl
{
    private readonly BackupWizardState _state;

    public RestoreStep3Target(BackupWizardState state)
    {
        InitializeComponent();
        _state = state;
        Loaded += (_, _) =>
        {
            var list = this.FindControl<ListBox>("DownloadDriveList");
            var vol = this.FindControl<TextBox>("RecoveryVolume");
            if (list != null) list.ItemsSource = DriveService.GetDrives();
            if (vol != null) { vol.Text = _state.RestoreRecoveryVolume; if (string.IsNullOrEmpty(vol.Text)) vol.Text = OperatingSystem.IsWindows() ? "C:" : "/"; }
        };
    }

    private void DownloadDriveList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (this.FindControl<ListBox>("DownloadDriveList")?.SelectedItem is DriveInfoModel d)
            _state.RestoreDownloadTargetDrive = d;
    }

    public bool IsValid()
    {
        var vol = this.FindControl<TextBox>("RecoveryVolume");
        _state.RestoreRecoveryVolume = (vol?.Text ?? "").Trim();
        if (string.IsNullOrEmpty(_state.RestoreRecoveryVolume))
            _state.RestoreRecoveryVolume = OperatingSystem.IsWindows() ? "C:" : "/";
        return _state.RestoreDownloadTargetDrive != null;
    }
}
