using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SnapVault.Models;
using SnapVault.Services;

namespace SnapVault.Views;

public partial class ScheduleDashboardView : UserControl
{
    private BackupWizardState _state = new();
    private readonly IBackupEngine _engine = BackupEngineFactory.Instance;
    private Action? _onChangeFullSchedule;
    private Action? _onChangeIncrementalSchedule;
    public event Action? BackToHomeRequested;

    public ScheduleDashboardView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        RefreshAll();
    }

    public void SetState(BackupWizardState state)
    {
        _state = state;
        RefreshAll();
    }

    public void SetScheduleChangeCallbacks(Action? onChangeFull, Action? onChangeIncremental)
    {
        _onChangeFullSchedule = onChangeFull;
        _onChangeIncrementalSchedule = onChangeIncremental;
    }

    public void RefreshAll()
    {
        UpdateScheduleSummary();
        RefreshHistory();
        RefreshUploadLog();
    }

    private void UpdateScheduleSummary()
    {
        var summaryText = this.FindControl<TextBlock>("ScheduleSummaryText");
        if (summaryText == null) return;
        var full = $"Full every {_state.FullBackupIntervalDays} days";
        var inc = _state.IncrementalIntervalHours == 0 ? "Incremental: Off" : $"Incremental every {_state.IncrementalIntervalHours}h";
        summaryText.Text = $"{full}; {inc}.";
    }

    private void RefreshHistory_OnClick(object? sender, RoutedEventArgs e) => RefreshHistory();

    private void RefreshHistory()
    {
        var list = this.FindControl<ItemsControl>("BackupHistoryList");
        var noHistory = this.FindControl<TextBlock>("NoHistoryText");
        if (list == null || noHistory == null) return;
        var target = _state.DestinationDrive?.Name?.TrimEnd('\\', '/');
        if (string.IsNullOrEmpty(target))
        {
            list.ItemsSource = null;
            noHistory.IsVisible = true;
            return;
        }
        var (ok, versions, _) = _engine.GetBackupVersions(target);
        var items = new List<BackupHistoryItem>();
        if (ok && versions != null)
            foreach (var v in versions)
            {
                var name = v.Identifier;
                var isInc = name.IndexOf("Incremental", StringComparison.OrdinalIgnoreCase) >= 0;
                items.Add(new BackupHistoryItem
                {
                    DisplayName = name,
                    TypeLabel = isInc ? "Incremental" : "Full",
                    DateDisplay = name
                });
            }
        list.ItemsSource = items;
        noHistory.IsVisible = items.Count == 0;
    }

    private void RefreshUploadLog()
    {
        var uploadList = this.FindControl<ItemsControl>("UploadLogList");
        var noUploads = this.FindControl<TextBlock>("NoUploadsText");
        var cloudSummary = this.FindControl<TextBlock>("CloudStatusSummary");
        if (uploadList == null || noUploads == null || cloudSummary == null) return;
        var entries = UploadLogService.ReadEntries();
        var hasCloud = !_state.KeepOnSourceDisk && _state.CloudConfig != null;
        cloudSummary.Text = hasCloud
            ? "Recent cloud uploads (when backup + cloud upload are enabled):"
            : "Cloud upload is not configured. Backups are stored on the destination only.";
        var display = entries.Select(e => new UploadLogDisplayItem
        {
            TimeDisplay = e.TimestampUtc.ToLocalTime().ToString("g"),
            StatusDisplay = e.Success ? "✓ Success" : "✗ Failed",
            Message = e.Message
        }).ToList();
        uploadList.ItemsSource = display;
        noUploads.IsVisible = display.Count == 0;
    }

    private void ChangeFullSchedule_OnClick(object? sender, RoutedEventArgs e) => _onChangeFullSchedule?.Invoke();
    private void ChangeIncremental_OnClick(object? sender, RoutedEventArgs e) => _onChangeIncrementalSchedule?.Invoke();
    private void BackToHome_OnClick(object? sender, RoutedEventArgs e) => BackToHomeRequested?.Invoke();

    private sealed class BackupHistoryItem
    {
        public string DisplayName { get; set; } = "";
        public string TypeLabel { get; set; } = "";
        public string DateDisplay { get; set; } = "";
    }

    private sealed class UploadLogDisplayItem
    {
        public string TimeDisplay { get; set; } = "";
        public string StatusDisplay { get; set; } = "";
        public string Message { get; set; } = "";
    }
}
