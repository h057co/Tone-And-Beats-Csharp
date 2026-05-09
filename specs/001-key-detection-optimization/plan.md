# Implementation Plan: Key Detection & UI Refinement

**Branch**: `001-key-detection-optimization` | **Date**: 2026-05-08 | **Spec**: [spec.md](file:///o:/Desarrollos/Tone%20And%20Beats%20Csharp/specs/001-key-detection-optimization/spec.md)

## Summary

Optimizing the musical key detection engine to support global tuning compensation (detecting offsets in cents from A=440Hz) and implementing a robust Gaussian Pitch Class Profile (PCP) analysis. The plan also includes a major UI upgrade for the Keyboard Overlay, featuring a 12-key chromatic piano with interactive key-swapping and a BPM-synced sine wave tone generator for scale verification. Loudness analysis parsing is refactored to handle interleaved FFmpeg logs for 100% reporting reliability.

## Technical Context

**Language/Version**: C# 12 / .NET 8.0-windows
**Primary Dependencies**: NAudio (for Tone Generation), SoundTouch.Net, FFMpegCore
**Storage**: N/A (Offline Analysis)
**Testing**: `KeyDetectorTest.csproj` (Console Test Harness)
**Target Platform**: Windows (WPF)
**Project Type**: Desktop Application
**Performance Goals**: < 1.0s analysis for 3min audio file
**Constraints**: Digital Brutalist UI adherence, no external cloud dependencies

## Constitution Check

| Principle | Assessment |
|-----------|------------|
| **Accuracy-First** | ✅ IMPLEMENTS Krumhansl-Schmuckler profiles and Gaussian soft-assignment for professional precision. |
| **Visual Excellence** | ✅ ENFORCES 1px borders, monospace fonts, and a chromatic piano layout for the keyboard overlay. |
| **Parallel Performance** | ✅ ENSURES analysis remains non-blocking via `Task.Run` and `async/await`. |
| **Standalone Reliability** | ✅ USES local NAudio processing; no external APIs. |
| **Data Integrity** | ✅ Metadata logic preserved; no destructive file operations. |

## Project Structure

### Documentation (this feature)

```text
specs/001-key-detection-optimization/
├── plan.md              # Implementation Plan
├── research.md          # MIR & DSP Decisions
├── data-model.md        # PCP & Key Entities
├── quickstart.md        # Test Scenarios
├── contracts/           # IToneGeneratorService
└── tasks.md             # Actionable Task List
```

### Source Code

```text
src/
├── Models/
│   └── LoudnessResult.cs (Updated with Error State)
├── Services/
│   ├── KeyDetector.cs (Gaussian PCP + Tuning)
│   ├── LoudnessAnalyzer.cs (Interleaved Output Parsing Fix)
│   └── ToneGeneratorService.cs [NEW]
├── ViewModels/
│   └── MainViewModel.cs (Key Swap Logic + Tone Sync)
└── MainWindow.xaml (Chromatic Piano Overlay + Bindings)
```

**Structure Decision**: Standard MVVM structure with service-layer extraction for DSP logic.

## Verification Plan

### Automated Tests
- `dotnet test KeyDetectorTest.csproj`: Verify accuracy against detuned synthetic signals (432Hz, 420Hz).
- Performance Benchmarks: Measure analysis time for 3min FLAC file in `AudioAnalyzer.PerfTest`.

### Manual Verification
- **Visual Audit**: Verify the piano overlay displays 12 chromatic keys with 1px borders and highlights the root/scale notes.
- **Audio Audit**: Verify the sine wave pulses at the track's BPM when "Play Scale" is enabled.
- **Loudness Audit**: Verify Integrated LUFS and True Peak are visible for tracks that previously failed to report results.
- **Interaction Check**: Verify clicking the detected key label swaps the results on both the UI and the piano visualizer.
