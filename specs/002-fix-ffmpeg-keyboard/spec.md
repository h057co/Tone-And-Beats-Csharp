# Feature Specification: FFmpeg Verification & Keyboard Fixes

**Feature Branch**: `002-fix-ffmpeg-keyboard`  
**Created**: 2026-05-08  
**Status**: Draft  
**Input**: User description: "1. vamos a verificar que el modulo FFmpeg este disponible, porque el modulo de Loudness no esta mostrando resultados. 2. aun el modulo de overlay keyboard no muestra la octaba de piano y tampoco esta reproduciendo la escala tonal detectada."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Verify FFmpeg Availability (Priority: P1)

As a user, I want the application to automatically verify and ensure that FFmpeg is available so that loudness analysis works correctly.

**Why this priority**: Loudness analysis is a core feature of the application. If FFmpeg is missing or broken, the module fails completely.

**Independent Test**: Can be fully tested by deleting the `ffmpeg` folder and verifying the application reports a "Module Missing" error, then restoring it and verifying successful analysis.

**Acceptance Scenarios**:

1. **Given** the application is started, **When** no FFmpeg binaries are found in the dependencies folder, **Then** the Loudness module displays an error status "FFmpeg Missing".
2. **Given** a valid audio file is loaded, **When** "Analyze" is clicked, **Then** the system uses FFmpeg to calculate LUFS and True Peak results.

---

### User Story 2 - Keyboard Piano Octave (Priority: P1)

As a user, I want to see a professional piano octave in the keyboard overlay that highlights the notes of the detected key.

**Why this priority**: Provides essential visual feedback for musical key detection and helps the user understand the detected scale.

**Independent Test**: Can be fully tested by loading files in different keys (e.g., C Major, A Minor) and verifying the correct piano keys are highlighted.

**Acceptance Scenarios**:

1. **Given** a key is detected (e.g., C Major), **When** the keyboard overlay is visible, **Then** it shows a 12-note chromatic octave with C, D, E, F, G, A, B highlighted.
2. **Given** the keyboard overlay is open, **When** no key is detected yet, **Then** it shows a blank piano octave with no highlights.

---

### User Story 3 - Tone Generator Scale Playback (Priority: P1)

As a user, I want to hear the notes of the detected scale played in sync with the BPM.

**Why this priority**: Auditory confirmation of the detected key and rhythm is critical for professional audio analysis.

**Independent Test**: Can be fully tested by enabling "Play Scale" and verifying that the resulting tones match the highlighted keys and follow the flashing BPM indicator.

**Acceptance Scenarios**:

1. **Given** a key and BPM are detected, **When** scale playback is activated, **Then** the system plays rhythmic pulses of the scale notes.
2. **Given** scale playback is active, **When** the BPM is changed (e.g., x2), **Then** the playback speed doubles accordingly.

---

### Edge Cases

- **What happens when FFmpeg exits with an error?**: The system should parse the stderr output and display a user-friendly error message in the ToolTip.
- **How does system handle a 0 BPM detection?**: Tone generator should either remain silent or default to a safe value (e.g., 120 BPM) with a warning.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST verify the existence of `ffmpeg.exe` and `ffprobe.exe` at startup.
- **FR-002**: System MUST capture and propagate FFmpeg process errors to the UI (LoudnessResult).
- **FR-003**: Keyboard Overlay MUST render a 12-note chromatic piano layout with distinct visuals for white and black keys.
- **FR-004**: System MUST highlight the notes belonging to the currently active musical scale (Major/Minor) on the piano.
- **FR-005**: Tone Generator MUST play a rhythmic sequence of sine wave pulses based on the active scale.
- **FR-006**: Tone Generator pulses MUST be synchronized with the internal BPM clock.

### Key Entities *(include if feature involves data)*

- **LoudnessResult**: Represents the parsed output from FFmpeg (LUFS, LRA, TP, ErrorState).
- **ScaleMap**: A binary mapping of the 12 chromatic notes representing which are active for a given key/mode.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Loudness module shows valid numeric results for 100% of supported audio formats when FFmpeg is present.
- **SC-002**: Piano visualization correctly maps 12 distinct chromatic positions with <100ms latency from key detection.
- **SC-003**: Audio tones are phase-accurate to the visual BPM flash (±10ms jitter).

## Assumptions

- **FFmpeg Binaries**: Assumed to be located in a `ffmpeg/` subdirectory relative to the application executable.
- **Audio Output**: Assumed the user has a valid default audio output device configured in Windows.
- **Permissions**: Assumed the application has read/write permissions to the `ffmpeg/` folder to check for existence.
