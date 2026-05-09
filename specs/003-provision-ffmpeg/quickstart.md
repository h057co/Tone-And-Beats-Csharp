# Quickstart: FFmpeg Provisioning

## Setup Developer Environment
To ensure all dependencies are present for local development:
1. Open PowerShell.
2. Run `.\scripts\setup-ffmpeg.ps1`.
3. Verify that `dependencies\ffmpeg\ffmpeg.exe` exists.

## Test Runtime Provisioning
To test the application's ability to recover from missing dependencies:
1. Delete the `ffmpeg` folder from the build output directory (e.g. `bin/Debug/net8.0-windows/ffmpeg`).
2. Run the application.
3. You should see a "Dependency Missing" overlay.
4. Click "Resolve" and wait for the download to complete.
5. Verify that Loudness analysis now works.
