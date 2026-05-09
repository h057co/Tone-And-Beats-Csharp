# Data Model: Dependency Provisioning

## Entities

### DependencyInfo
Represents an external dependency that needs to be provisioned.

| Field | Type | Description |
|-------|------|-------------|
| Name | string | e.g. "FFmpeg" |
| DownloadUrl | string | URL to fetch the archive |
| TargetFolder | string | Where to extract the files |
| Status | enum | Idle, Downloading, Extracting, Ready, Error |
| Progress | double | 0 to 100 |
| ErrorMessage | string | Details if Status == Error |

## State Transitions

- **Idle** → **Downloading**: Triggered by user or automatic check.
- **Downloading** → **Extracting**: Triggered upon successful stream completion.
- **Extracting** → **Ready**: Triggered after file I/O operations complete.
- **Extracting** → **Error**: Triggered if extraction fails (e.g. disk full, permission denied).
