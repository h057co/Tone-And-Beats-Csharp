# Feature Specification: Key Detection & UI Refinement

**Feature Branch**: `001-key-detection-optimization`  
**Created**: 2026-05-08  
**Status**: Draft  
**Input**: User description: "Revisa modulo del overlay Keyboard, debe tener la apariencia de una octava de un piano y mostrar resaltando las teclas de la tonalidad detectada y elegida en el momento, también desde el detected key en el overlay al darle click se pueda cambiar al key alternativo del resultado y en el piano se vea reflejado resaltado. revisa la función del reproductor de tonalidad en el modulo overlay de keyboard, el generador de tono senoidal no esta funcionando, este generador debe reproducir la tonalidad detectada y seleccionada en el momento al bpm que se ha detectado la canción. el modulo de loudness no esta funcionando, no se ven los resultados."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Analyze Non-Standard Audio (Priority: P1)
As a DJ, I want to load tracks with non-standard reference frequencies (e.g., 432Hz or 420Hz) so that I can see the correct musical key without manual pitch correction.

**Why this priority**: Professional-grade precision requires handling the reality of diverse audio sources.
**Independent Test**: Load a synthetic C Major chord generated at 420Hz and verify the system detects it as B Major (nearest semitone) or C Major (compensated).
**Acceptance Scenarios**:
1. **Given** an audio file tuned to A=432Hz, **When** analyzed, **Then** the system detects a ~32 cent flat offset and identifies the musical key correctly.
2. **Given** an audio file tuned to A=440Hz, **When** analyzed, **Then** the system detects 0 cents offset.

---

### User Story 2 - Keyboard Visuals & Interaction (Priority: P1)
As a producer, I want to see a realistic chromatic piano keyboard in the overlay that highlights the detected key, and I want to be able to toggle between primary and alternative results by clicking the UI.

**Why this priority**: Visual verification and the ability to correct automated detection are critical for professional workflow.
**Independent Test**: Open the keyboard overlay, click the "Detected Key" label, and verify the highlighted piano keys update to reflect the alternative key result.
**Acceptance Scenarios**:
1. **Given** a detected key (e.g., C Major) and an alternative (e.g., G Major), **When** the keyboard overlay is open, **Then** the C Major scale keys are highlighted on a chromatic piano layout.
2. **Given** the alternative result is available, **When** the user clicks the detected key label, **Then** the results swap and the piano highlighting updates instantly.

---

### User Story 3 - BPM-Synced Tone Playback (Priority: P2)
As a DJ, I want to hear a sine wave tone pulsing at the track's BPM to verify the detected key and tempo alignment.

**Why this priority**: Auditory confirmation provides a second layer of verification beyond the visual display.
**Independent Test**: Enable "Play Scale" on a track with a known BPM (e.g., 120) and verify the sine wave pulses exactly 120 times per minute.
**Acceptance Scenarios**:
1. **Given** a track analyzed at 124 BPM, **When** scale playback is enabled, **Then** a sine wave at the root note frequency pulses at 124 BPM.

---

### User Story 4 - Reliable Loudness Reporting (Priority: P1)
As a mastering engineer, I want to see accurate LUFS and True Peak results after analysis so that I can judge the dynamic range of the track.

**Why this priority**: Loudness normalization is a core requirement for broadcasting and streaming standards.
**Independent Test**: Analyze a track with known loudness (e.g., -14.0 LUFS) and verify the Integrated LUFS display matches the reference.
**Acceptance Scenarios**:
1. **Given** an audio file, **When** analysis is complete, **Then** Integrated LUFS, LRA, and True Peak are displayed (no generic "--" or error state).

### Edge Cases
- **Silent Audio**: Tone generator should not play or should play at a default silent frequency if no key is detected.
- **Extreme BPM**: At very high BPM (e.g., 200+), the pulsing should remain distinct and not turn into a continuous hum.
- **Parsing Errors**: If FFmpeg fails, the UI MUST show an "ERROR" state instead of silent failure.

## Requirements *(mandatory)*

### Functional Requirements
- **FR-001**: System MUST detect tuning offsets in cents relative to A=440Hz.
- **FR-002**: System MUST apply tuning offsets to the frequency-to-pitch mapping.
- **FR-003**: System MUST provide a chromatic piano visualizer (7 white, 5 black keys per octave).
- **FR-004**: System MUST highlight the root and scale degrees of the selected key on the piano.
- **FR-005**: System MUST allow swapping Primary/Alternative keys via UI interaction.
- **FR-006**: System MUST pulse a sine wave at the root note frequency at the detected BPM.
- **FR-007**: System MUST parse Integrated LUFS, LRA, and True Peak from FFmpeg output with 99% reliability.
- **FR-008**: System MUST provide visual error feedback if any analysis module (Key, BPM, Loudness) fails.

### Key Entities
- **Tuning Offset**: Deviation in cents from A=440Hz.
- **Piano Octave**: A visual representation of 12 chromatic notes.
- **Tone Pulse**: A BPM-synchronized amplitude modulation of a sine wave.

## Success Criteria *(mandatory)*

### Measurable Outcomes
- **SC-001**: Tuning accuracy within +/- 5 cents for clear tonal signals.
- **SC-002**: Key swapping interaction completes in < 100ms.
- **SC-003**: Loudness results displayed for 100% of successfully decoded files.
- **SC-004**: Tone generator frequency matches the selected key within 0.1Hz.

## Assumptions
- **A4=440Hz**: Default reference.
- **NAudio Integration**: Using NAudio for tone generation is possible within the current WPF context.
- **FFmpeg Presence**: FFmpeg is available and accessible for loudness analysis.
