using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SnapVault.Services;

namespace SnapVault.Views;

public partial class PrepareFullRestoreView : UserControl
{
    public event Action? BackToHomeRequested;

    private string? _lastSavedScriptPath;

    public PrepareFullRestoreView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (TargetPath != null)
            TargetPath.Text = OperatingSystem.IsMacOS() ? "/Volumes/Macintosh HD" : "/mnt/root";
        RefreshBackupFolders();
        InstructionsPreview.Text = FullSystemRestoreService.GetInstructions(OperatingSystem.IsMacOS(), FullSystemRestoreService.ScriptFileName);
    }

    private static string ToBackupRoot(string? userPath)
    {
        if (string.IsNullOrWhiteSpace(userPath)) return "";
        var p = userPath.Trim().TrimEnd('/', '\\');
        return p.EndsWith("SnapVault", StringComparison.OrdinalIgnoreCase) ? p : Path.Combine(p, "SnapVault");
    }

    private void RefreshBackupFolders()
    {
        var backupRoot = ToBackupRoot(BackupRootPath?.Text);
        if (string.IsNullOrEmpty(backupRoot))
        {
            BackupFolderCombo.ItemsSource = null;
            BackupFolderCombo.PlaceholderText = "Enter backup path above, then select (or use Latest)";
            return;
        }
        var (ok, folders, _) = FullSystemRestoreService.GetBackupFolders(backupRoot);
        if (ok && folders.Count > 0)
        {
            var list = new List<string> { "(Latest)" };
            list.AddRange(folders);
            BackupFolderCombo.ItemsSource = list;
            BackupFolderCombo.SelectedIndex = 0;
        }
        else
        {
            BackupFolderCombo.ItemsSource = null;
            BackupFolderCombo.PlaceholderText = "No backup folders found, or path invalid. Use Latest when running from live media.";
        }
    }

    private void Generate_OnClick(object? sender, RoutedEventArgs e)
    {
        var backupRoot = ToBackupRoot(BackupRootPath?.Text);
        var target = TargetPath?.Text?.Trim();
        if (string.IsNullOrEmpty(backupRoot))
        {
            ShowStatus("Enter the backup location (drive or path that contains your SnapVault backup).", true);
            return;
        }
        if (string.IsNullOrEmpty(target))
        {
            ShowStatus("Enter the restore target path (e.g. /mnt/root or /Volumes/Macintosh HD).", true);
            return;
        }

        string? backupFolder = null;
        if (BackupFolderCombo?.SelectedItem is string s && !s.Equals("(Latest)", StringComparison.OrdinalIgnoreCase))
            backupFolder = s;

        var script = FullSystemRestoreService.GenerateRestoreScript(backupRoot, backupFolder, target, OperatingSystem.IsMacOS());
        var instructions = FullSystemRestoreService.GetInstructions(OperatingSystem.IsMacOS(), FullSystemRestoreService.ScriptFileName);
        InstructionsPreview.Text = instructions;

        _ = SaveScriptAndInstructionsAsync(script, instructions);
    }

    private async Task SaveScriptAndInstructionsAsync(string scriptContent, string instructionsContent)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var options = new FilePickerSaveOptions
        {
            Title = "Save restore script",
            SuggestedFileName = FullSystemRestoreService.ScriptFileName,
            DefaultExtension = "sh",
            FileTypeChoices = new[] { new FilePickerFileType("Shell script") { Patterns = new[] { "*.sh" } } }
        };

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(options);
        if (file == null) return;

        try
        {
            await using (var stream = await file.OpenWriteAsync())
            using (var writer = new StreamWriter(stream))
                await writer.WriteAsync(scriptContent);

            var dir = Path.GetDirectoryName(file.Path.LocalPath);
            if (!string.IsNullOrEmpty(dir))
            {
                var instructionsPath = Path.Combine(dir, FullSystemRestoreService.InstructionsFileName);
                await File.WriteAllTextAsync(instructionsPath, instructionsContent);
            }

            _lastSavedScriptPath = file.Path.LocalPath;
            ShowStatus($"Script saved to: {file.Path.LocalPath}\nInstructions saved in the same folder. Boot from live media and run the script with sudo.", false);
        }
        catch (Exception ex)
        {
            ShowStatus("Error saving: " + ex.Message, true);
        }
    }

    private async void SaveToFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        var backupRoot = ToBackupRoot(BackupRootPath?.Text) ?? "";
        var target = TargetPath?.Text?.Trim() ?? "/mnt/root";
        string? backupFolder = BackupFolderCombo?.SelectedItem is string s && !s.Equals("(Latest)", StringComparison.OrdinalIgnoreCase) ? s : null;
        var script = FullSystemRestoreService.GenerateRestoreScript(backupRoot, backupFolder, target, OperatingSystem.IsMacOS());
        var instructions = FullSystemRestoreService.GetInstructions(OperatingSystem.IsMacOS(), FullSystemRestoreService.ScriptFileName);

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var options = new FolderPickerOpenOptions { Title = "Choose folder (e.g. USB drive)" };
        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(options);
        if (folders.Count == 0) return;

        var dir = folders[0].Path.LocalPath;
        try
        {
            var scriptPath = Path.Combine(dir, FullSystemRestoreService.ScriptFileName);
            var instructionsPath = Path.Combine(dir, FullSystemRestoreService.InstructionsFileName);
            await File.WriteAllTextAsync(scriptPath, script);
            await File.WriteAllTextAsync(instructionsPath, instructions);
            ShowStatus($"Script and instructions saved to: {dir}\nInsert this USB when you boot from live media and run: sudo ./" + FullSystemRestoreService.ScriptFileName, false);
        }
        catch (Exception ex)
        {
            ShowStatus("Error saving to folder: " + ex.Message, true);
        }
    }

    private void ShowStatus(string message, bool isError)
    {
        if (StatusText == null) return;
        StatusText.IsVisible = true;
        StatusText.Text = message;
        StatusText.Foreground = Avalonia.Media.SolidColorBrush.Parse(isError ? "#E07C6A" : "#8BC34A");
    }

    private void BackupRootPath_LostFocus(object? sender, RoutedEventArgs e) => RefreshBackupFolders();

    private void BackToHome_OnClick(object? sender, RoutedEventArgs e) => BackToHomeRequested?.Invoke();
}
