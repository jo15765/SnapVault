using Avalonia.Controls;
using Avalonia.Interactivity;
using SnapVault.Models;

namespace SnapVault.Views;

public partial class Step3StorageChoice : UserControl
{
    private readonly BackupWizardState _state;

    public Step3StorageChoice(BackupWizardState state)
    {
        _state = state;
        InitializeComponent();
    }

    private void KeepLocal_OnChecked(object? sender, RoutedEventArgs e)
    {
        _state.KeepOnSourceDisk = true;
        _state.CloudConfig = null;
        CloudPanel.IsVisible = false;
    }

    private void MoveToCloud_OnChecked(object? sender, RoutedEventArgs e)
    {
        _state.KeepOnSourceDisk = false;
        CloudPanel.IsVisible = true;
        _state.CloudConfig ??= new CloudStorageConfig();
        SyncCloudConfig();
    }

    private void SyncCloudConfigFromUi(object? sender, RoutedEventArgs e) => SyncCloudConfig();

    private void SyncCloudConfig()
    {
        if (_state?.CloudConfig == null) return;
        _state.CloudConfig.Provider = ProviderCombo.SelectedIndex == 0 ? "Azure" : "S3";
        _state.CloudConfig.ContainerOrBucket = ContainerBucket.Text ?? "";
        _state.CloudConfig.AccountNameOrKeyId = AccountKeyId.Text ?? "";
        _state.CloudConfig.SecretKey = SecretKey.Text ?? "";
        _state.CloudConfig.EndpointOrRegion = EndpointRegion.Text ?? "";
        _state.UploadToCloudAfterBackup = UploadAfterBackup.IsChecked == true;
    }

    public bool IsValid()
    {
        if (_state.KeepOnSourceDisk) return true;
        SyncCloudConfig();
        if (_state.CloudConfig == null) return false;
        return !string.IsNullOrWhiteSpace(_state.CloudConfig.ContainerOrBucket)
               && !string.IsNullOrWhiteSpace(_state.CloudConfig.AccountNameOrKeyId)
               && !string.IsNullOrWhiteSpace(_state.CloudConfig.SecretKey);
    }
}
