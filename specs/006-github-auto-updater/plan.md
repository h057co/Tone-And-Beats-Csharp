# Implementation Plan: GitHub Auto-Updater

Implement a robust, manual-trigger auto-updater that connects to GitHub Releases, downloads updates in the background, and performs a "silent" replacement on restart, adhering to the Digital Brutalist aesthetic.

## User Review Required

> [!IMPORTANT]
> The update process requires launching a temporary PowerShell process to replace the locked `.exe` file. This is the standard pattern for Single-File apps but may trigger Windows Defender "SmartScreen" warnings if the binary isn't signed.

## Proposed Changes

### [Component] Services Layer

#### [NEW] UpdateService.cs
- Implement `IUpdateService`.
- Methods: `CheckForUpdatesAsync()`, `DownloadUpdateAsync(IProgress<double> progress)`, `ApplyUpdateAndRestart()`.
- Logic:
  - Fetch from `api.github.com`.
  - Compare semantic versions.
  - Download `.exe` asset to `Path.GetTempFileName()`.

### [Component] UI Layer

#### [MODIFY] AboutWindow.xaml
- Add a dedicated "Update" section.
- **Brutalist Elements**:
  - 1px border container for update info.
  - Status indicator: `ESTADO: AL DÍA` (Green) or `ESTADO: ACTUALIZACIÓN DISPONIBLE (v1.2.1)` (Cyan).
  - Button: `[ BUSCAR ACTUALIZACIONES ]`.

#### [MODIFY] AboutWindow.xaml.cs
- Handle button click.
- Show progress bar during download.
- Show "REINICIAR PARA ACTUALIZAR" button once download is complete.

### [Component] Infrastructure

#### [MODIFY] AudioAnalyzer.csproj
- Ensure `Version` is updated to `1.2.0` (current baseline).
- The updater will use this `Version` tag for comparison.

## Constitution Check

- **Accuracy-First**: The updater must accurately compare versions using standard .NET comparison logic.
- **Visual Excellence**: The update status and buttons must match the Digital Brutalist theme (Consolas font, 0px radius).
- **Parallel Performance**: All network calls MUST be async to avoid freezing the About window.
- **Standalone Reliability**: The update process will rely on `powershell.exe` which is a standard Windows component.

## Verification Plan

### Automated Tests
- Unit test for `VersionComparisonLogic`.
- Mock GitHub API responses for "Higher Version", "Same Version", and "Network Error" scenarios.

### Manual Verification
1. Open About window.
2. Click "BUSCAR ACTUALIZACIONES".
3. Verify "Al día" message appears if version matches.
4. Temporarily lower the version in code.
5. Verify "Actualización Disponible" appears.
6. Trigger download and verify progress bar.
7. Click "Reiniciar" and verify the app closes, a terminal flicker appears, and the app re-opens.
