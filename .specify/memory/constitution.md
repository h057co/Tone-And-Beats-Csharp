<!--
Sync Impact Report:
- Version change: 1.0.0 → 1.1.0
- List of modified principles:
  - Visual Excellence & UX → Now explicitly mandates the "Digital Brutalist" aesthetic.
- Added sections:
  - Design System Tokens (GridUnit, MainPadding).
- Templates requiring updates:
  - .specify/templates/plan-template.md (✅ updated)
- Follow-up TODOs: None.
-->

# Tone & Beats by Hostility Constitution

## Core Principles

### I. Accuracy-First Analysis
All audio detection engines (BPM, Key, Loudness) must prioritize high-precision results over processing speed. Accuracy must be verified against professional standards (e.g., Krumhansl-Schmuckler for keys, multi-engine voting for BPM). The goal is to provide results that DJs and producers can trust implicitly.

### II. Visual Excellence & UX (Digital Brutalist mandate)
The user interface must adhere to the **Digital Brutalist** aesthetic:
- **Typography**: Strict use of monospace fonts (Consolas, Lucida Console) for all technical data.
- **Borders**: 1px sharp borders with `0px` corner radius (no rounded corners).
- **Layout**: High-density information distribution using the standardized `GridUnit` (8px) and `MainPadding` (12px) tokens.
- **Interactions**: Smooth micro-animations (e.g., Storyboard-based fade-ins) are REQUIRED for all modal/overlay elements to maintain a premium feel.

### III. Parallel Performance
Audio analysis must be non-blocking and utilize parallel processing (Tasks/Async) to ensure the UI remains responsive even when processing large files. Heavy computations should never lock the main UI thread.

### IV. Standalone Reliability
The application must be self-contained. All critical dependencies (ffmpeg, ffprobe) must be bundled or verified at build/runtime. No external cloud dependencies are allowed for core analysis features to ensure privacy and offline functionality.

### V. Data Integrity (Metadata)
Metadata writing (ID3v2, BWF) must be safe and atomic. The application must never corrupt source files. Support for industry-standard DJ software tags (Serato, Rekordbox, Traktor) is a priority.

## Technology Stack

- **Core**: .NET 8 / C# 12
- **UI**: WPF (Windows Presentation Foundation) with SkiaSharp for custom rendering.
- **Design System**: BrutalistTheme.xaml with dynamic tokens (`GridUnit`, `MainPadding`, `ControlPadding`).
- **Audio Logic**: NAudio (IO/Playback), FFMpegCore (LUFS/Metadata), SoundTouch.Net (Tempo/Pitch).
- **Architecture**: MVVM (Model-View-ViewModel).

## Quality Assurance

- **Performance Audits**: Periodic verification via `AudioAnalyzer.PerfTest` to ensure no regressions in speed or memory usage.
- **Accuracy Verification**: Manual and automated checks of BPM/Key results using reference tracks with known values.
- **Theme Testing**: Visual verification across all supported themes, ensuring strict adherence to the 1px border rule.

## Governance

Amendments to these principles require a version bump in the constitution. All new feature proposals must be validated against the "Accuracy-First" and "Visual Excellence" principles. PRs should not be merged if they violate the Standalone Reliability principle or use non-compliant UI elements (e.g., rounded buttons).

**Version**: 1.1.0 | **Ratified**: 2026-05-08 | **Last Amended**: 2026-05-08
