<!--
Sync Impact Report:
- Version change: 1.1.0 → 1.2.0
- List of modified principles:
  - Accuracy-First Analysis → Explicitly mentioned Essentia for high-precision BPM and PCP for keys.
  - Parallel Performance & Decoupled Architecture → Formally added DI as a core architectural requirement.
  - Standalone Reliability → Added Essentia to the list of bundled binaries.
  - Data Integrity → Added TagLibSharp as the standard for metadata.
- Added sections: None.
- Templates requiring updates:
  - .specify/templates/plan-template.md (✅ updated)
  - .specify/templates/spec-template.md (✅ updated)
  - .specify/templates/tasks-template.md (✅ updated)
- Follow-up TODOs: None.
-->

# Tone & Beats by Hostility Constitution

## Core Principles

### I. Accuracy-First Analysis
All audio detection engines (BPM, Key, Loudness) must prioritize high-precision results over processing speed. Accuracy must be verified against professional standards. 
- **BPM**: Use Essentia's `streaming_extractor_music` for production-grade tempo detection.
- **Key**: Multi-algorithm verification (e.g., Krumhansl-Schmuckler, PCP) to ensure reliability for harmonic mixing.
The goal is to provide results that DJs and producers can trust implicitly.

### II. Visual Excellence & UX (Digital Brutalist mandate)
The user interface must adhere to the **Digital Brutalist** aesthetic:
- **Typography**: Strict use of monospace fonts (Consolas, Lucida Console) for all technical data.
- **Borders**: 1px sharp borders with `0px` corner radius (no rounded corners).
- **Layout**: High-density information distribution using the standardized `GridUnit` (8px) and `MainPadding` (12px) tokens.
- **Interactions**: Smooth micro-animations (e.g., Storyboard-based fade-ins) are REQUIRED for all modal/overlay elements to maintain a premium feel.

### III. Parallel Performance & Decoupled Architecture
Audio analysis must be non-blocking and utilize parallel processing (Tasks/Async). 
- **Decoupling**: All core logic must be encapsulated in services registered via `Microsoft.Extensions.DependencyInjection`.
- **UI Responsiveness**: Heavy computations should never lock the main UI thread. 
- **Testability**: Services must be interface-driven to allow for unit testing and mocking.

### IV. Standalone Reliability
The application must be self-contained. All critical dependencies (`ffmpeg`, `ffprobe`, `essentia`) must be bundled and verified at build/runtime. No external cloud dependencies are allowed for core analysis features to ensure privacy and offline functionality.

### V. Data Integrity (Metadata)
Metadata writing (ID3v2, BWF) must be safe and atomic. The application must never corrupt source files. Support for industry-standard DJ software tags (Serato, Rekordbox, Traktor) is a priority using `TagLibSharp` and custom mapping.

## Technology Stack

- **Core**: .NET 8 / C# 12
- **UI**: WPF (Windows Presentation Foundation) with SkiaSharp for custom rendering.
- **Design System**: BrutalistTheme.xaml with dynamic tokens (`GridUnit`, `MainPadding`, `ControlPadding`).
- **Audio Logic**: NAudio (IO/Playback), FFMpegCore (LUFS/Metadata), Essentia (High-Precision BPM), SoundTouch.Net (Pitch), TagLibSharp (Metadata).
- **Architecture**: MVVM with Dependency Injection (`Microsoft.Extensions.DependencyInjection`).

## Quality Assurance

- **Performance Audits**: Periodic verification via `AudioAnalyzer.PerfTest` to ensure no regressions in speed or memory usage.
- **Accuracy Verification**: Automated checks of BPM/Key results using reference tracks with known values.
- **Theme Testing**: Visual verification across all supported themes, ensuring strict adherence to the 1px border rule.
- **Dependency Validation**: Build-time checks to ensure all required binaries are present in the `dependencies/` folder.

## Governance

Amendments to these principles require a version bump in the constitution. All new feature proposals must be validated against the "Accuracy-First" and "Visual Excellence" principles. PRs should not be merged if they violate the Standalone Reliability principle or use non-compliant UI elements (e.g., rounded buttons).

**Version**: 1.2.0 | **Ratified**: 2026-05-08 | **Last Amended**: 2026-05-15
