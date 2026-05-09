# Research: Key Detection, Tone Generation & UI Layout

## Decisions

### 1. Key Profiles: Krumhansl-Schmuckler (K-S)
- **Decision**: Use K-S profiles for semantic correlation.
- **Rationale**: Industry standard for Western tonal music detection. Provides high accuracy for Major/Minor modes.

### 2. Tone Generation: NAudio SineWaveProvider
- **Decision**: Use `NAudio`'s `ISampleProvider` with a custom `SineWaveProvider32`.
- **Rationale**: Low latency and easy to integrate with a BPM-synced amplitude envelope.

### 3. BPM Synchronization: Amplitude Gating
- **Decision**: Use a `DispatcherTimer` set to `60000 / BPM` intervals to toggle the amplitude of the `SineWaveProvider`.
- **Rationale**: Simple to implement in WPF and provides clear auditory pulses without complex buffer modulation.

### 4. Piano Layout: Chromatic 12-Note Octave
- **Decision**: Implement a 12-key chromatic layout (7 white, 5 black) using a custom `Grid` with absolute positioning for black keys.
- **Rationale**: Ensures musical accuracy and adheres to the Digital Brutalist aesthetic for information density.

### 5. Loudness Parsing: Robust Regex
- **Decision**: Scan combined stdout/stderr for specific JSON keys (`input_i`, `input_tp`, `input_lra`).
- **Rationale**: Fixes the issue where status messages interleave with results, causing standard JSON parsers to fail.

## Unknowns Resolved
- **Tone Frequency**: Derived from MIDI note: `440.0 * Math.Pow(2, (midiNote - 69) / 12.0)`.
- **BPM Pulse**: 50ms decay envelope to prevent clicks during amplitude gating.
