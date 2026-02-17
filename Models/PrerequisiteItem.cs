namespace SnapVault.Models;

/// <summary>
/// A required piece of software that may be missing, with install actions.
/// </summary>
public class PrerequisiteItem
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsMissing { get; set; }
    /// <summary>Optional: run this to trigger install (e.g. xcode-select --install).</summary>
    public string? InstallCommand { get; set; }
    /// <summary>Optional: arguments for InstallCommand.</summary>
    public string? InstallArguments { get; set; }
    /// <summary>Optional: URL to open for manual install (e.g. .NET download page).</summary>
    public string? DownloadUrl { get; set; }
    /// <summary>Label for the install button, e.g. "Install via system dialog".</summary>
    public string InstallButtonLabel { get; set; } = "Install";
}
