# SnapVault

**Cross-platform backup and restore** for Windows, Linux, and macOS. Create full or incremental system backups, schedule them, optionally upload to cloud (Azure Blob or S3), and restore—with a single desktop app.

Built with **[Avalonia UI](https://avaloniaui.net/)** and **.NET 8**.

---

## Features

| Feature | Windows | Linux | macOS |
|--------|---------|--------|--------|
| Full system backup | wbAdmin (VSS snapshot) | rsync | rsync |
| Incremental backup | wbAdmin | rsync + `--link-dest` | rsync + `--link-dest` |
| Scheduling | Task Scheduler | cron | launchd |
| Cloud upload | Azure Blob, S3 | Azure Blob, S3 | Azure Blob, S3 |
| Restore from cloud | wbAdmin recovery | rsync to target | rsync to target |
| Full restore from live media | WinRE + wbadmin | Generated script + any live USB | Generated script + installer/recovery |

- **Backup while you work** (Windows uses Volume Shadow Copy; no need to leave the PC idle).
- **Schedule dashboard** — After setup, the app stays in the taskbar. Open it to change full/incremental schedule, view **backup history**, and **cloud upload status**.
- **Full system restore (Linux/macOS)** — Generate a step-by-step restore script to run from live media. The script reports progress at each step (root check, locate backup, rsync with progress, completion).

---

## Requirements

- **.NET 8.0 SDK** (to build) or **.NET 8.0 Runtime** (to run)
- **Windows**: Administrator rights for backup/restore. Windows Backup (wbadmin) must be available.
- **Linux**: `rsync` (e.g. `apt install rsync`). Cron is used for scheduling.
- **macOS**: `rsync` (Xcode Command Line Tools). launchd is used for scheduling.

---

## Quick start

```bash
git clone <your-repo-url>
cd BackupWizardApp
dotnet restore
dotnet build
dotnet run --project SnapVault
```

The app opens to **Backup** or **Restore**. On Linux/macOS, **Prepare full system restore (live media)** is also available from the home screen.

---

## Usage overview

### Backup

1. **Select source** — Drive to back up (with used/free space).
2. **Select destination** — Where the backup is stored (with size estimate).
3. **Storage choice** — Keep on destination only, or upload to **Azure Blob** or **Amazon S3** (optional) after each backup.
4. **Schedule** — Full backup interval (1–30 days) and incremental interval (Off / 12–48 hours). Click **Finish** to run the first backup and create the schedule.

After the first backup, the app shows the **schedule dashboard**: change full/incremental schedule, view backup history, and cloud upload status. The app remains in the taskbar for quick access.

### Restore (from cloud)

1. Enter **cloud credentials** (Azure or S3).
2. **Select backup set** (refresh from cloud, then pick one).
3. Choose **download location** and **recover-to path** (e.g. `C:` on Windows, `/` or `/mnt/root` on Linux/macOS).
4. **Finish** — Backup is downloaded, then recovery runs (wbAdmin on Windows, rsync on Linux/macOS).

### Full system restore (Linux/macOS, from live media)

Use **Prepare full system restore (live media)** from the home screen. Enter backup location and target path, then generate and save the script (and instructions) to a folder or USB. Boot from any Linux or macOS live environment, mount backup and target, and run:

```bash
sudo ./snapvault-full-restore.sh
```

The script prints **[Step 1/5]** through **[Step 5/5]** and rsync progress so you can follow each step.

---

## Technical notes

- **Config**: `%APPDATA%\SnapVault\config.json` (Windows), `~/.config/SnapVault/` or equivalent (Linux), `~/Library/Application Support/SnapVault/` (macOS).
- **Backup layout (Linux/macOS)**: Under the destination path, `SnapVault/Full_<timestamp>/` and `SnapVault/Incremental_<timestamp>/`.
- **Cloud**: Full backup sets are uploaded so they can be restored on any platform. Credentials are stored in the config file; protect the machine and consider using secrets managers in production.

---

## Project structure

```
BackupWizardApp/
├── SnapVault/                 # Main Avalonia app
│   ├── Views/                 # UI (wizard steps, dashboard, dialogs)
│   ├── Services/              # Backup engines, cloud, config, restore script generator
│   ├── Models/                # State, drive info, cloud config
│   └── Styles.axaml
├── README.md
└── LICENSE
```

---

## License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.
