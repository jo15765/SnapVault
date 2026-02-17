using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SnapVault.Models;
using SnapVault.Services;

namespace SnapVault.Views;

public partial class Step4Schedule : UserControl
{
    private readonly BackupWizardState _state;

    public Step4Schedule(BackupWizardState state)
    {
        InitializeComponent();
        _state = state;
        Loaded += OnLoaded;
        FullBackupDays.SelectionChanged += (_, _) => UpdateStateAndSummary();
        IncrementalInterval.SelectionChanged += (_, _) => UpdateStateAndSummary();
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // Sync dropdown from state (e.g. loaded config)
        IncrementalInterval.SelectedIndex = _state.IncrementalIntervalHours switch
        {
            0 => 0,
            12 => 1,
            24 => 2,
            36 => 3,
            48 => 4,
            _ => 2
        };
        InUseHint.Text = OperatingSystem.IsWindows()
            ? "Windows uses a volume snapshot (VSS), so full and incremental backups can run while you work; no need to leave the PC idle."
            : "Backups run live (rsync). For best consistency, schedule full/incremental runs when the system is relatively idle (e.g. overnight).";
        UpdateStateAndSummary();
    }

    private void UpdateStateAndSummary()
    {
        _state.FullBackupIntervalDays = FullBackupDays.SelectedIndex switch
        {
            0 => 1,
            1 => 3,
            2 => 7,
            3 => 14,
            4 => 30,
            _ => 7
        };
        _state.IncrementalIntervalHours = IncrementalInterval.SelectedIndex switch
        {
            0 => 0,
            1 => 12,
            2 => 24,
            3 => 36,
            4 => 48,
            _ => 24
        };

        var src = _state.SourceDrive?.Name ?? "—";
        var dst = _state.DestinationDrive?.Name ?? "—";
        var sizeStr = _state.SourceDrive != null ? DriveInfoModel.FormatBytes(_state.EstimatedBackupSizeBytes) : "—";
        var storage = _state.KeepOnSourceDisk ? "Destination only" : "Upload to cloud after each backup";
        var incText = _state.IncrementalIntervalHours == 0 ? "Incremental: Off" : $"Incremental every {_state.IncrementalIntervalHours}h";

        SummarySource.Text = src;
        SummarySourceSize.Text = _state.SourceDrive != null ? $" — {sizeStr}" : "";
        SummaryDestination.Text = dst;
        SummaryImageName.Text = "Full backup";
        SummaryImageSize.Text = _state.SourceDrive != null ? $" — {sizeStr}" : "";
        SummarySchedule.Text = $"Full every {_state.FullBackupIntervalDays} days; {incText}.";
        SummaryStorage.Text = $"Storage: {storage}.";
    }
}
