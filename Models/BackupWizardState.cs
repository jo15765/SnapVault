namespace SnapVault.Models;

public class BackupWizardState
{
    public DriveInfoModel? SourceDrive { get; set; }
    public DriveInfoModel? DestinationDrive { get; set; }
    public long EstimatedBackupSizeBytes { get; set; }
    public bool KeepOnSourceDisk { get; set; } = true;
    public CloudStorageConfig? CloudConfig { get; set; }
    public int FullBackupIntervalDays { get; set; } = 7;
    /// <summary>Incremental backup interval in hours; 0 = off. Supported: 0, 12, 24, 36, 48.</summary>
    public int IncrementalIntervalHours { get; set; } = 24;
    public bool UploadToCloudAfterBackup { get; set; }

    // Restore flow
    public CloudBackupSetInfo? RestoreSelectedBackupSet { get; set; }
    public DriveInfoModel? RestoreDownloadTargetDrive { get; set; }
    public string RestoreRecoveryVolume { get; set; } = "C:"; // volume to recover to
}

public class CloudBackupSetInfo
{
    public string Id { get; set; } = string.Empty;       // e.g. "Backup 2024-02-15 120000"
    public string DisplayName { get; set; } = string.Empty;
    public bool IsIncremental { get; set; }
    public DateTime? Date { get; set; }
    public string TypeLabel => IsIncremental ? "Incremental" : "Full";
}

public class CloudStorageConfig
{
    public string Provider { get; set; } = "Azure"; // Azure | S3
    public string EndpointOrRegion { get; set; } = string.Empty;
    public string ContainerOrBucket { get; set; } = string.Empty;
    public string AccountNameOrKeyId { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public bool UseHttps { get; set; } = true;
}

public class UploadLogEntry
{
    public DateTime TimestampUtc { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? BackupFolder { get; set; }
}
