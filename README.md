# Tone & Beats

**Tone & Beats** is a professional-grade audio analysis application designed with a high-precision BPM engine and a unique Digital Brutalist aesthetic.

## Features

- **High-Precision BPM Detection**: Powered by Essentia Beta 5 for superior tempo extraction, especially in urban genres.
- **Musical Key Analysis**: Full-spectrum logarithmic Chroma (PCP) analysis with global tuning compensation.
- **Loudness & Waveform Visualization**: Real-time analysis and visualization of audio characteristics.
- **Cyberpunk/Terminal UI**: A professional, high-contrast interface designed for data-heavy musical analysis.
- **GitHub Auto-Updater**: Integrated silent background update mechanism.

## Tech Stack

- **Core**: .NET 8 / WPF
- **Audio Engines**: Essentia, FFmpeg
- **Design**: Custom "Digital Brutalist" LookAndFeel
- **Infrastructure**: GitHub Releases for updates
- **Architecture**: Modular Dependency Injection (DI) with Service-Oriented logic extraction.

## Core Services

- **PlaybackController**: Encapsulated audio playback management.
- **AnalysisOrchestrator**: Unified audio analysis pipeline.
- **KeyDisplayService**: Musical theory and scale visualization engine.
- **ILoggerService**: Injectable logging system for improved testability.

## Setup

1. Clone the repository.
2. Ensure you have the .NET 8 SDK installed.
3. Dependencies (Essentia/FFmpeg) are managed within the `dependencies/` folder.
4. Open `AudioAnalyzer.sln` and build.

## Project Status

Current Version: **v1.2.0**

---
*Developed by Hostility - Military-Grade Audio Tools.*
