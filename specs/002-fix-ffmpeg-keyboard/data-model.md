# Data Model: FFmpeg Verification & Keyboard Fixes

This document defines the data structures and validation rules for the FFmpeg and Keyboard modules.

## 1. LoudnessResult (Model)
Updated to support error propagation.

| Field | Type | Description |
|-------|------|-------------|
| Integrated | double | Integrated Loudness in LUFS |
| LRA | double | Loudness Range in LU |
| TruePeak | double | True Peak in dBFS |
| HasError | bool | True if analysis failed |
| ErrorMessage | string | Detailed error message from FFmpeg |

## 2. ScaleMap (Entity)
Represents a musical scale mapped to a 12-note chromatic octave.

| Field | Type | Description |
|-------|------|-------------|
| RootNote | int | MIDI note index (0-11, where 0 is C) |
| Mode | string | Major or Minor |
| Notes | bool[12] | Active notes in the octave |

### Validation Rules
- **RootNote**: Must be between 0 and 11.
- **Notes**: Must have exactly 12 elements.
- **BPM**: (External) Must be > 0 for rhythmic playback.

## 3. KeyboardState (View State)
| Field | Type | Description |
|-------|------|-------------|
| IsVisible | bool | Toggles the keyboard overlay |
| HighlightedNotes | bool[12] | Notes currently active on the visual piano |
| SelectedKey | string | Text display (e.g., "Cmaj") |
