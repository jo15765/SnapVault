using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace SnapVault.Views;

public partial class ModeSelectView : UserControl
{
    public event Action? BackupSelected;
    public event Action? RestoreSelected;
    public event Action? DashboardRequested;
    public event Action? PrepareFullRestoreRequested;

    public ModeSelectView()
    {
        InitializeComponent();
    }

    public void SetDashboardVisible(bool visible)
    {
        if (DashboardButton != null)
            DashboardButton.IsVisible = visible;
    }

    public void SetPrepareFullRestoreVisible(bool visible)
    {
        if (PrepareFullRestoreButton != null)
            PrepareFullRestoreButton.IsVisible = visible;
    }

    private void PrepareFullRestore_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        PrepareFullRestoreRequested?.Invoke();
    }

    private void DashboardButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        DashboardRequested?.Invoke();
    }

    private void BackupCard_OnClick(object? sender, PointerPressedEventArgs e)
    {
        BackupSelected?.Invoke();
    }

    private void RestoreCard_OnClick(object? sender, PointerPressedEventArgs e)
    {
        RestoreSelected?.Invoke();
    }
}
