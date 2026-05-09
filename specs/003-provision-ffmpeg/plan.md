# Implementation Plan: FFmpeg Binary Provisioning

## Technical Context

We need to ensure FFmpeg is always available for the Loudness module.
- **Current State**: `AudioAnalyzer.csproj` expects FFmpeg in `..\dependencies\ffmpeg\`. If missing, build validation fails (if enabled) or the app fails at runtime.
- **Goal**: Automate the provisioning of these binaries for both developers and end-users.

### Architecture Decoupling
The provisioning logic will be split into:
1. **Developer Script**: A PowerShell script to bootstrap the local environment.
2. **Runtime Bootstrapper**: Logic in the app to handle missing binaries (e.g. downloading them).

## Constitution Check

| Principle | Adherence | Notes |
|-----------|-----------|-------|
| **Accuracy-First** | ✅ | Ensures the loudness engine (which uses FFmpeg) is functional. |
| **Visual Excellence** | ✅ | The resolution UI will use Digital Brutalist tokens. |
| **Standalone Reliability** | ✅ | Directly addresses the requirement for self-contained dependencies. |
| **Data Integrity** | ✅ | N/A (no file mutation in this feature). |

## Gates

- [x] FFmpeg source is reliable (Gyan.dev / GitHub).
- [x] Binaries are for Windows x64.

## Phase 0: Research

### [Decision]: Use Gyan.dev for automated downloads.
- **Rationale**: It is the industry standard for Windows FFmpeg builds.
- **Alternatives**: Compiling from source (too slow/complex), or using BtbN/FFmpeg-Builds on GitHub (also viable).

## Phase 1: Design & Contracts

### [NEW] [scripts/setup-ffmpeg.ps1](file:///o:/Desarrollos/Tone%20And%20Beats%20Csharp/scripts/setup-ffmpeg.ps1)
A script to download, extract, and place FFmpeg in `../dependencies/ffmpeg/`.

### [MODIFY] [MainViewModel.cs](file:///o:/Desarrollos/Tone%20And%20Beats%20Csharp/ViewModels/MainViewModel.cs)
Add a check at startup and a command to trigger the setup process if missing.

### [NEW] [Services/DependencyService.cs](file:///o:/Desarrollos/Tone%20And%20Beats%20Csharp/Services/DependencyService.cs)
A service to handle the download and extraction of external dependencies.

## Phase 2: Implementation Tasks

- [ ] T001 [P] Create `scripts/setup-ffmpeg.ps1` to automate developer environment setup.
- [ ] T002 [P] Implement `DependencyService.cs` with `DownloadFFmpegAsync` method.
- [ ] T003 Update `MainViewModel` to check for FFmpeg at startup.
- [ ] T004 Create a "Dependency Missing" overlay in `MainWindow.xaml` using Digital Brutalist styling.
- [ ] T005 Verify the end-to-end flow by deleting the `ffmpeg` folder and running the app.
