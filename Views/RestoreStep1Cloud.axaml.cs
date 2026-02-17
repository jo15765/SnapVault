using Avalonia.Controls;
using Avalonia.Interactivity;
using SnapVault.Models;
using SnapVault.Services;

namespace SnapVault.Views;

public partial class RestoreStep1Cloud : UserControl
{
    private readonly BackupWizardState _state;

    public RestoreStep1Cloud(BackupWizardState state)
    {
        _state = state;
        InitializeComponent();
        Loaded += (_, _) =>
        {
            var saved = BackupConfigService.Load();
            if (saved?.CloudConfig != null)
            {
                var c = saved.CloudConfig;
                ProviderCombo.SelectedIndex = c.Provider.Equals("S3", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                ContainerBucket.Text = c.ContainerOrBucket;
                AccountKeyId.Text = c.AccountNameOrKeyId;
                SecretKey.Text = c.SecretKey;
                EndpointRegion.Text = c.EndpointOrRegion;
                SyncConfig(null!, null!);
            }
        };
    }

    private void SyncConfig(object? sender, RoutedEventArgs e)
    {
        if (_state == null) return;
        // Don't read from UI until controls exist (events can fire during InitializeComponent)
        if (ProviderCombo == null) return;
        _state.CloudConfig ??= new CloudStorageConfig();
        _state.CloudConfig.Provider = ProviderCombo.SelectedIndex == 0 ? "Azure" : "S3";
        _state.CloudConfig.ContainerOrBucket = ContainerBucket?.Text ?? "";
        _state.CloudConfig.AccountNameOrKeyId = AccountKeyId?.Text ?? "";
        _state.CloudConfig.SecretKey = SecretKey?.Text ?? "";
        _state.CloudConfig.EndpointOrRegion = EndpointRegion?.Text ?? "";
    }

    public bool IsValid()
    {
        SyncConfig(null!, null!);
        return _state.CloudConfig != null
               && !string.IsNullOrWhiteSpace(_state.CloudConfig.ContainerOrBucket)
               && !string.IsNullOrWhiteSpace(_state.CloudConfig.AccountNameOrKeyId)
               && !string.IsNullOrWhiteSpace(_state.CloudConfig.SecretKey);
    }
}
