# Research: FFmpeg Verification & Keyboard Fixes

This document outlines the technical decisions and research findings for the FFmpeg module verification and Keyboard Overlay stabilization.

## 1. FFmpeg Loudness Analysis Parsing

### Decision: Dual-Output Regex Parsing
**Rationale**: FFmpeg's `loudnorm` filter can output results in two primary ways: a structured JSON block (when using `print_format=json`) or a summary text block. To ensure maximum reliability, the analyzer will implement a dual-parsing strategy.
- **JSON Parsing**: Prefer `print_format=json` for machine-readability.
- **Summary Regex**: Fallback to Regex for summary output if JSON is unavailable or malformed.
  - Pattern: `Integrated: ([\d.-]+) LUFS`
  - Pattern: `LRA: ([\d.-]+) LU`
  - Pattern: `True Peak: ([\d.-]+) dBTP`

### Alternatives Considered
- **Direct Library Integration**: Rejected because `FFMpegCore` is already used and provides a stable process wrapper. Writing a custom C++ wrapper for `libavfilter` would violate the "Complexity Tracking" principle (excessive maintenance).

## 2. WPF Chromatic Piano Layout

### Decision: UniformGrid with Layered Overlay
**Rationale**: The 12-note octave will be implemented using a base `UniformGrid` for the 7 white keys, with 5 black keys positioned as a layered `Canvas` or `Grid` overlay using absolute positioning or specific margins.
- **Visuals**: Strict 1px borders, 0px corner radius per the Digital Brutalist mandate.
- **Highlights**: Use `DataTriggers` bound to a `bool[]` array in the ViewModel (representing the 12 chromatic semitones).

### Alternatives Considered
- **Custom Rendered SkiaSharp Control**: Overkill for a simple piano layout. Standard WPF elements provide easier binding for highlighting.

## 3. Rhythmic Tone Playback (NAudio)

### Decision: Gated Sample Provider with Linear Decay
**Rationale**: To avoid audio clicks and ensure rhythmic accuracy, we will use a `GatedSampleProvider` that wraps a `SineWaveProvider`.
- **Timing**: Use a `DispatcherTimer` or a background Task with `ManualResetEvent` for millisecond-precision triggers synchronized with the UI BPM flash.
- **Envelope**: A 50ms linear decay (fade-out) will be applied to each pulse to ensure musicality.

### Alternatives Considered
- **MIDI Output**: Rejected as it requires external synth mapping. Sine wave pulses keep the application "Standalone Reliable".
