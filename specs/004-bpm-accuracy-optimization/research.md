# Research: BPM Accuracy & Urban Strategy

## Decisions

### 1. MIR Engine: Essentia CLI (Static Binary)
- **Decision**: Integrate `essentia_streaming_extractor_music.exe` as a bundled dependency.
- **Rationale**: 
  - `RhythmExtractor2013` is an industry standard for high-precision beat tracking.
  - Using the CLI avoids complex C++ interop/marshalling issues in .NET.
  - Consistent with the FFmpeg provisioning strategy already established in the project.
- **Alternatives considered**: 
  - **SoundTouch**: Currently used, but lacks sophisticated rhythmic analysis for urban genres.
  - **Native C++ Wrapper**: Rejected due to high maintenance cost and potential memory safety issues.

### 2. Urban Strategy Heuristic (Trap/Reggaetón)
- **Decision**: Implement a post-analysis "Disambiguation Service" in C#.
- **Rationale**: 
  - If Essentia detects ~140 BPM, we analyze the **Onset Density** and **Snare Placement**.
  - **70 BPM (Trap)**: Snare on beat 3, high-density hi-hats (1/16+).
  - **140 BPM (Techno/House)**: Snare on 2 & 4, lower-density hats.
  - We will use the `BeatTimesSeconds` provided by Essentia to verify these patterns.

### 3. Data Persistence
- **Decision**: Use `System.Text.Json` for serializing the `BpmAnalysisResult`.
- **Rationale**: Lightweight, built-in to .NET 8, and easy to bind to WPF UI.

## Research Findings

### Essentia CLI Output Format
The `essentia_streaming_extractor_music.exe` produces a JSON file containing:
```json
{
  "rhythm": {
    "bpm": 120.4,
    "beats_position": [0.5, 1.0, 1.5, ...],
    "beats_confidence": 0.85
  }
}
```
We will parse this into our internal model.

### Trap Snare Placement Logic
By analyzing the amplitude of audio segments at specific beat positions (using NAudio), we can identify if the "strongest" non-kick hit occurs on beat 3 (suggesting 70 BPM) or beats 2 & 4 (suggesting 140 BPM).

## Unresolved Clarifications
- **Binary Size**: Bundling Essentia might increase the installer size by ~20MB. (Accepted as per Constitution I: Accuracy-First).
- **Performance**: Running an external process adds overhead. (Accepted as per Constitution I: Accuracy-First).
