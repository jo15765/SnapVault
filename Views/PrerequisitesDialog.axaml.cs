using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SnapVault.Models;
using SnapVault.Services;

namespace SnapVault.Views;

public partial class PrerequisitesDialog : Window
{
    public PrerequisitesDialog()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void SetMissingItems(IEnumerable<PrerequisiteItem> items)
    {
        var list = this.FindControl<ItemsControl>("MissingItemsList");
        if (list != null)
            list.ItemsSource = items;
    }

    private async void InstallButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.DataContext is not PrerequisiteItem item)
            return;

        btn.IsEnabled = false;
        var statusText = this.FindControl<TextBlock>("StatusText");
        try
        {
            if (statusText != null)
            {
                statusText.IsVisible = true;
                statusText.Text = "Starting...";
            }

            var progress = new Progress<string>(s =>
            {
                if (statusText != null)
                    statusText.Text = s;
            });

            var (success, error) = await PrerequisiteChecker.TryInstallAsync(item, progress);

            if (statusText != null)
            {
                statusText.Text = success
                    ? "Done. You may need to restart the app for changes to take effect."
                    : ("Failed: " + (error ?? "Unknown error"));
            }
        }
        finally
        {
            btn.IsEnabled = true;
        }
    }

    private void OpenUrlButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: PrerequisiteItem item } && !string.IsNullOrEmpty(item.DownloadUrl))
            PrerequisiteChecker.TryOpenUrl(item.DownloadUrl);
    }

    private void ContinueButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
