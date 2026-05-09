# Research: GitHub Auto-Updater

## Decision: GitHub API v3 + Custom Background Updater

### Rationale
- **GitHub API v3**: It is the industry standard for querying public repository releases. It provides a clean JSON response with `tag_name` and `browser_download_url` for assets.
- **Silent Update Pattern**: In a Single-File Windows environment, files are often locked during execution. A direct self-replacement is impossible. The standard approach is to download the new `.exe` to a temporary location, then launch a separate "Updater" script (PowerShell or a small utility) that waits for the main process to exit, copies the new file, and restarts the app.

### Alternatives Considered
- **Squirrel.Windows**: Powerful but heavy and adds significant complexity to the build process. Not ideal for a "Brutalist" minimalist app.
- **ClickOnce**: Restricted to Microsoft's deployment model, doesn't play well with custom Single-File installers like Inno Setup.
- **WinGet**: Requires users to have WinGet installed and the app to be in the community repository. Too much friction for an "independent" DJ tool.

## Key Findings

### 1. GitHub API Access
- **Endpoint**: `https://api.github.com/repos/HostilityMusic/ToneAndBeats/releases/latest`
- **Authentication**: No token required for public repos (subject to rate limits, which is fine for manual checks).
- **User-Agent**: MUST provide a User-Agent header (e.g., `ToneAndBeatsUpdater`) or GitHub will reject the request.

### 2. Semantic Versioning in .NET
- `System.Version` can handle `1.2.0.0` but might fail on `v1.2.0`.
- We should strip the 'v' prefix if present before parsing.

### 3. Silent Update Workflow (The "Handshake")
1. Main App checks for update.
2. If found, Main App downloads the new `.exe` to `%TEMP%\tone_beats_update.exe`.
3. User clicks "Apply Update".
4. Main App writes a small `update.bat` or runs a PowerShell command that:
   - Loops until `ToneAndBeatsByHostility.exe` is closed.
   - Replaces the old `.exe` with the new one.
   - Re-launches the app.
   - Deletes itself.

## Best Practices
- **Timeout**: Set a 10s timeout for the check to avoid blocking.
- **Verification**: If possible, verify the hash of the downloaded file (though not strictly required for v1).
- **UI Feedback**: Show "Checking..." → "New version found!" or "Up to date".
