using System.Text.Json.Serialization;

namespace SnapVault.Models;

public class DriveInfoModel
{
    public string Name { get; set; } = string.Empty;
    public string VolumeLabel { get; set; } = string.Empty;
    public string FileSystem { get; set; } = string.Empty;
    public long TotalBytes { get; set; }
    public long FreeBytes { get; set; }

    [JsonIgnore]
    public long UsedBytes => TotalBytes - FreeBytes;

    [JsonIgnore]
    public string TotalFormatted => FormatBytes(TotalBytes);
    [JsonIgnore]
    public string FreeFormatted => FormatBytes(FreeBytes);
    [JsonIgnore]
    public string UsedFormatted => FormatBytes(UsedBytes);
    [JsonIgnore]
    public string DisplayName => string.IsNullOrWhiteSpace(VolumeLabel)
        ? $"{Name} ({TotalFormatted} total)"
        : $"{Name} {VolumeLabel} ({TotalFormatted} total)";

    public static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double len = bytes;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
