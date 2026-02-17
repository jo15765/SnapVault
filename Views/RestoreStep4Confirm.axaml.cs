using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SnapVault.Models;

namespace SnapVault.Views;

public partial class RestoreStep4Confirm : UserControl
{
    private readonly BackupWizardState _state;

    public RestoreStep4Confirm(BackupWizardState state)
    {
        InitializeComponent();
        _state = state;
        RestoreRequirementsHint.Text = OperatingSystem.IsWindows()
            ? "Windows: Run this app as Administrator. Restoring the system drive (C:) uses wbadmin; a restart may be required. For bare-metal or failed-boot recovery, use Windows Recovery Environment (WinRE) and run wbadmin start sysrecovery from there."
            : "Linux/macOS: Restoring over root (/) while the system is running can skip in-use files. For full system restore, boot from live media and run rsync from the downloaded backup folder. Restoring to a data path or second drive works from a running system.";
        Loaded += (_, _) => UpdateSummary();
    }

    public void UpdateSummary()
    {
        var backup = _state.RestoreSelectedBackupSet?.DisplayName ?? "—";
        var drive = _state.RestoreDownloadTargetDrive?.Name ?? "—";
        var vol = _state.RestoreRecoveryVolume;
        SummaryText.Text = $"Backup: {backup}\nDownload to: {drive}\nRecover to: {vol}\n\nDownload will start, then recovery will run.";
    }
}
