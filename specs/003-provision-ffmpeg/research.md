# Research: FFmpeg Provisioning

## Unknowns & Clarifications

### Q1: Which FFmpeg build to use?
- **Decision**: Use `ffmpeg-git-full.7z` or `ffmpeg-release-essentials.zip` from Gyan.dev.
- **Rationale**: Essentials contains everything needed for `loudnorm` without being overly large (~35MB compressed).
- **URL**: `https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip`

### Q2: How to handle 7z/ZIP in C# without dependencies?
- **Decision**: Use `System.IO.Compression.ZipFile` for ZIP files (available in .NET 8).
- **Rationale**: Keeps the application lightweight and avoids extra NuGet packages.

### Q3: Where to store the binaries at runtime?
- **Decision**: The application directory under a `ffmpeg/` folder.
- **Rationale**: Consistent with current `FindFFmpeg()` logic and `AudioAnalyzer.csproj` content inclusion.

## Integration Patterns

- **Background Download**: Use `HttpClient.GetStreamAsync` with a progress reporting wrapper.
- **UI Interaction**: Use a non-blocking modal overlay (Brutalist style) to inform the user and show progress.
