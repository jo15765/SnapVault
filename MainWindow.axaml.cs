using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using SnapVault.Models;
using SnapVault.Services;
using SnapVault.Views;

namespace SnapVault;

public partial class MainWindow : Window
{
    private enum ViewMode { ModeSelect, Backup, Restore }
    private ViewMode _mode = ViewMode.ModeSelect;
    private int _currentStep;
    private readonly BackupWizardState _state = new();
    private readonly UserControl[] _backupSteps;
    private readonly UserControl[] _restoreSteps;
    private readonly ModeSelectView _modeSelectView;
    private readonly ScheduleDashboardView _dashboardView;
    private readonly IBackupEngine _engine = BackupEngineFactory.Instance;

    public MainWindow()
    {
        InitializeComponent();
        _backupSteps = new UserControl[]
        {
            new Step1SelectSource(_state),
            new Step2SelectDestination(_state),
            new Step3StorageChoice(_state),
            new Step4Schedule(_state)
        };
        _restoreSteps = new UserControl[]
        {
            new RestoreStep1Cloud(_state),
            new RestoreStep2SelectBackup(_state),
            new RestoreStep3Target(_state),
            new RestoreStep4Confirm(_state)
        };
        _modeSelectView = new ModeSelectView();
        _modeSelectView.BackupSelected += () => StartWizard(ViewMode.Backup);
        _modeSelectView.RestoreSelected += () => StartWizard(ViewMode.Restore);
        _modeSelectView.DashboardRequested += ShowDashboard;
        _modeSelectView.PrepareFullRestoreRequested += ShowPrepareFullRestore;
        _dashboardView = new ScheduleDashboardView();
        _dashboardView.SetScheduleChangeCallbacks(OpenEditScheduleDialog, OpenEditScheduleDialog);
        _dashboardView.BackToHomeRequested += ShowModeSelect;
        ShowModeSelect();
        NextButton.IsVisible = false;
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= MainWindow_Loaded;
        var missing = PrerequisiteChecker.CheckAll().Where(x => x.IsMissing).ToList();
        if (missing.Count == 0) return;
        var dialog = new PrerequisitesDialog
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };
        dialog.SetMissingItems(missing);
        await dialog.ShowDialog(this);
    }

    private void ShowModeSelect()
    {
        _mode = ViewMode.ModeSelect;
        StepIndicatorPanel.IsVisible = false;
        StepContent.Content = _modeSelectView;
        BackButton.IsVisible = false;
        NextButton.IsVisible = false;
        SwitchModeButton.IsVisible = false;
        _modeSelectView.SetDashboardVisible(BackupConfigService.Load()?.DestinationDrive != null);
        _modeSelectView.SetPrepareFullRestoreVisible(!OperatingSystem.IsWindows());
    }

    private void StartWizard(ViewMode mode)
    {
        _mode = mode;
        _currentStep = 0;
        StepIndicatorPanel.IsVisible = true;
        SwitchModeButton.IsVisible = true;
        SwitchModeButton.Content = _mode == ViewMode.Backup ? "Switch to Restore" : "Switch to Backup";
        var steps = _mode == ViewMode.Backup ? _backupSteps : _restoreSteps;
        StepContent.Content = steps[0];
        BackButton.IsVisible = false;
        NextButton.IsVisible = true;
        NextButton.Content = steps.Length == 1 ? "Finish" : "Next →";
        UpdateStepIndicator();
    }

    private void ShowStep(int index)
    {
        _currentStep = index;
        SwitchModeButton.Content = _mode == ViewMode.Backup ? "Switch to Restore" : "Switch to Backup";
        var steps = _mode == ViewMode.Backup ? _backupSteps : _restoreSteps;
        StepContent.Content = steps[index];
        BackButton.IsVisible = index > 0;
        NextButton.Content = index == steps.Length - 1 ? "Finish" : "Next →";
        UpdateStepIndicator();
        UpdateNextButtonState();
    }

    private void SwitchModeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ShowModeSelect();
    }

    internal void UpdateNextButtonState()
    {
        NextButton.IsEnabled = _mode == ViewMode.ModeSelect || CanAdvanceFrom(_currentStep);
    }

    private void UpdateStepIndicator()
    {
        var accent = SolidColorBrush.Parse("#E8A838");
        var card = SolidColorBrush.Parse("#1A2332");
        Step1Border.Background = _currentStep == 0 ? accent : card;
        Step2Border.Background = _currentStep == 1 ? accent : card;
        Step3Border.Background = _currentStep == 2 ? accent : card;
        Step4Border.Background = _currentStep == 3 ? accent : card;
    }

    private async void NextButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var steps = _mode == ViewMode.Backup ? _backupSteps : _restoreSteps;
        if (!CanAdvanceFrom(_currentStep))
            return;
        if (_currentStep < steps.Length - 1)
        {
            if (_currentStep == 3 && _mode == ViewMode.Restore && StepContent.Content is RestoreStep4Confirm confirm)
                confirm.UpdateSummary();
            ShowStep(_currentStep + 1);
            return;
        }

        if (_mode == ViewMode.Backup)
            await RunBackupFinishAsync();
        else
            await RunRestoreFinishAsync();
    }

    private async System.Threading.Tasks.Task RunBackupFinishAsync()
    {
        NextButton.IsEnabled = false;
        try
        {
            BackupConfigService.Save(_state);
            var targetPath = _state.DestinationDrive?.Name ?? "";
            var sourcePath = _state.SourceDrive?.Name ?? "";

            var (backupOk, backupOut, backupErr) = await System.Threading.Tasks.Task.Run(() =>
                _engine.RunFullBackup(sourcePath, targetPath)).ConfigureAwait(true);

            if (!backupOk)
            {
                await DialogHelper.ShowWarningAsync("Backup", $"Backup did not complete.\n\nError: {backupErr}\n\nOutput: {backupOut}");
                NextButton.IsEnabled = true;
                return;
            }

            var (scheduleOk, scheduleMsg) = _engine.ScheduleBackups(_state, targetPath);
            if (!scheduleOk)
                await DialogHelper.ShowWarningAsync("Schedule", scheduleMsg);
            else
                await DialogHelper.ShowAsync("Backup Wizard", $"Backup completed.\n\n{scheduleMsg}");

            if (!_state.KeepOnSourceDisk && _state.CloudConfig != null && _state.UploadToCloudAfterBackup)
            {
                var folder = _engine.GetLatestBackupFolder(targetPath);
                if (!string.IsNullOrEmpty(folder))
                {
                    var (uploadOk, uploadMsg) = await CloudStorageService.UploadBackupSetAsync(
                        targetPath, folder, _state.CloudConfig, null).ConfigureAwait(true);
                    if (uploadOk)
                    {
                        await DialogHelper.ShowAsync("Cloud Upload", $"Uploaded: {uploadMsg}");
                        UploadLogService.LogUpload(new UploadLogEntry
                        {
                            TimestampUtc = DateTime.UtcNow,
                            Success = true,
                            Message = uploadMsg,
                            BackupFolder = folder
                        });
                    }
                    else
                    {
                        await DialogHelper.ShowWarningAsync("Cloud Upload", uploadMsg);
                        UploadLogService.LogUpload(new UploadLogEntry
                        {
                            TimestampUtc = DateTime.UtcNow,
                            Success = false,
                            Message = uploadMsg,
                            BackupFolder = folder
                        });
                    }
                }
            }

            ShowDashboard();
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowErrorAsync("Backup Wizard", ex.Message);
        }
        finally
        {
            NextButton.IsEnabled = true;
        }
    }

    private async System.Threading.Tasks.Task RunRestoreFinishAsync()
    {
        if (_state.CloudConfig == null || _state.RestoreSelectedBackupSet == null || _state.RestoreDownloadTargetDrive == null)
        {
            await DialogHelper.ShowWarningAsync("Restore", "Please complete all restore steps.");
            return;
        }
        NextButton.IsEnabled = false;
        try
        {
            var drive = _state.RestoreDownloadTargetDrive.Name.TrimEnd('\\', '/');
            var setId = _state.RestoreSelectedBackupSet.Id;
            var vol = _state.RestoreRecoveryVolume;
            if (string.IsNullOrEmpty(vol)) vol = _engine.IsWindows ? "C:" : "/";

            var (downloadOk, downloadMsg) = await CloudStorageService.DownloadBackupSetAsync(
                setId, drive, _state.CloudConfig, null).ConfigureAwait(true);
            if (!downloadOk)
            {
                await DialogHelper.ShowErrorAsync("Restore", $"Download failed: {downloadMsg}");
                NextButton.IsEnabled = true;
                return;
            }

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                await DialogHelper.ShowAsync("Restore", "Backup downloaded. Starting recovery..."));

            var (versOk, versions, versOut) = _engine.GetBackupVersions(drive);
            if (!versOk || versions.Count == 0)
            {
                await DialogHelper.ShowWarningAsync("Restore", $"Could not get backup versions from {drive}. {versOut}");
                NextButton.IsEnabled = true;
                return;
            }

            var versionId = versions[0].Identifier;
            var (recOk, recOut, recErr) = _engine.StartRecovery(drive, versionId, vol, vol);
            if (!recOk)
                await DialogHelper.ShowWarningAsync("Restore", $"{recErr}\n{recOut}");
            else
                await DialogHelper.ShowAsync("Restore", "Recovery completed. A restart may be required on Windows.");

            Close();
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowErrorAsync("Restore", ex.Message);
        }
        finally
        {
            NextButton.IsEnabled = true;
        }
    }

    private void ShowDashboard()
    {
        var loaded = BackupConfigService.Load();
        if (loaded != null)
        {
            _state.FullBackupIntervalDays = loaded.FullBackupIntervalDays;
            _state.IncrementalIntervalHours = loaded.IncrementalIntervalHours;
            _state.DestinationDrive = loaded.DestinationDrive;
            _state.SourceDrive = loaded.SourceDrive;
            _state.CloudConfig = loaded.CloudConfig;
            _state.UploadToCloudAfterBackup = loaded.UploadToCloudAfterBackup;
            _state.KeepOnSourceDisk = loaded.KeepOnSourceDisk;
        }
        _mode = ViewMode.ModeSelect;
        StepIndicatorPanel.IsVisible = false;
        StepContent.Content = _dashboardView;
        BackButton.IsVisible = false;
        NextButton.IsVisible = false;
        SwitchModeButton.IsVisible = false;
        _dashboardView.SetState(_state);
        _dashboardView.RefreshAll();
    }

    private void ShowPrepareFullRestore()
    {
        var view = new PrepareFullRestoreView();
        view.BackToHomeRequested += () => { StepContent.Content = _modeSelectView; ShowModeSelect(); };
        StepIndicatorPanel.IsVisible = false;
        StepContent.Content = view;
        BackButton.IsVisible = false;
        NextButton.IsVisible = false;
        SwitchModeButton.IsVisible = false;
    }

    private async void OpenEditScheduleDialog()
    {
        var dialog = new EditScheduleDialog(_state) { WindowStartupLocation = WindowStartupLocation.CenterOwner };
        await dialog.ShowDialog(this);
        _dashboardView.SetState(_state);
        _dashboardView.RefreshAll();
    }

    private void BackButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_currentStep > 0)
            ShowStep(_currentStep - 1);
        else
            ShowModeSelect();
    }

    private bool CanAdvanceFrom(int step)
    {
        if (_mode == ViewMode.Backup)
        {
            return step switch
            {
                0 => (_backupSteps[0] as Step1SelectSource)?.IsValid() == true,
                1 => (_backupSteps[1] as Step2SelectDestination)?.IsValid() == true,
                2 => (_backupSteps[2] as Step3StorageChoice)?.IsValid() == true,
                3 => true,
                _ => true
            };
        }
        return step switch
        {
            0 => (_restoreSteps[0] as RestoreStep1Cloud)?.IsValid() == true,
            1 => (_restoreSteps[1] as RestoreStep2SelectBackup)?.IsValid() == true,
            2 => (_restoreSteps[2] as RestoreStep3Target)?.IsValid() == true,
            3 => true,
            _ => true
        };
    }
}
