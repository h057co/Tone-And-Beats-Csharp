# Data Model: GitHub Auto-Updater

## Entities

### 1. UpdateRelease (GitHub Release Metadata)
Represents the data retrieved from the GitHub API.

| Field | Type | Description |
| :--- | :--- | :--- |
| `TagName` | String | The version tag (e.g., "v1.2.1"). |
| `Name` | String | The release title. |
| `ReleaseNotes` | String | The `body` of the release from GitHub (Markdown). |
| `DownloadUrl` | String | The URL for the primary `.exe` or `.zip` asset. |
| `PublishedAt` | DateTime | When the release was made public. |

### 2. UpdateState (Enum)
Tracks the lifecycle of the update process.

- `Idle`: No check has been performed.
- `Checking`: API request in progress.
- `UpdateAvailable`: New version found, awaiting user action.
- `Downloading`: Asset is being retrieved.
- `ReadyToInstall`: Asset downloaded, restart required.
- `UpToDate`: Current version is the latest.
- `Error`: Something went wrong (offline, rate limit).

## Logic Rules

### Version Comparison
- Use `System.Version.Parse()` on both current and remote versions.
- Current Version: Extracted from `Assembly.GetExecutingAssembly().GetName().Version`.
- Remote Version: Strip 'v' prefix from `TagName`.
- Comparison: `remoteVersion > currentVersion`.

### Storage
- No persistent storage needed for the release itself.
- A local "SkipVersion" can be stored in the existing `AppSettings` (if available) or a simple text file.
