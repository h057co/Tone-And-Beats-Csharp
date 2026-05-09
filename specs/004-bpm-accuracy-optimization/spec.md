# Feature Specification: BPM Accuracy & Urban Strategy Optimization

**Feature Branch**: `004-bpm-accuracy-optimization`  
**Created**: 2026-05-09  
**Status**: Draft  
**Input**: User description: "añade la mejora de incorporar el Urban Strategy y Data model sugeridos en la propuesta"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - High-Precision Urban Tempo Analysis (Priority: P1)

As a professional DJ or producer working with urban genres (Trap, Reggaetón, Drill), I want the BPM detection engine to correctly identify the "musical tempo" (e.g., 70 BPM instead of the technical 140 BPM) by analyzing rhythmic density (kick/snare/hat patterns), so that the beat grid aligns with the perceived groove of the track.

**Why this priority**: Correct tempo classification is critical for synchronization in modern urban genres where technical tempo and musical groove often differ by a 2:1 ratio.

**Independent Test**: Run the `AudioAnalyzer.PerfTest` audit against the `audiotest` folder, specifically targeting Trap tracks (e.g., audio5, audio6). Verify that the "Reinterpreted" flag is correctly set and the primary BPM matches the ground truth.

**Acceptance Scenarios**:

1. **Given** a Trap track with a technical tempo of 140 BPM, **When** analyzed, **Then** the primary BPM is 70 BPM and the `ReinterpretedHalfTime` flag is TRUE.
2. **Given** a Reggaetón track with a steady dembow, **When** analyzed, **Then** the primary BPM matches the technical pulse (e.g., 90 BPM) with high confidence.

---

### User Story 2 - Advanced Rhythmic Data Export (Priority: P2)

As a developer or power user, I want the analysis to provide more than just a single BPM number, including exact beat timestamps and intervals, so that I can generate precise visual waveforms and metronome clicks that never drift over time.

**Why this priority**: Professional audio workflows require a sample-accurate "Beat Grid" rather than just a static BPM estimation.

**Independent Test**: Analyze a 4-minute track and verify that the `BeatTimesSeconds` list contains timestamps consistent with the detected BPM for the entire duration.

**Acceptance Scenarios**:

1. **Given** a successful analysis, **When** examining the data model, **Then** `BeatTimesSeconds` and `BeatIntervals` are populated with sub-millisecond precision.
2. **Given** a variable tempo track (audio 6), **When** analyzed, **Then** the `BeatIntervals` accurately reflect the local tempo changes.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST implement a **Urban Strategy** heuristic that uses kick/snare periodicity and hat density to resolve half-time/double-time ambiguities (e.g., 70 vs 140 BPM).
- **FR-002**: System MUST utilize a high-precision MIR engine (e.g., Essentia `RhythmExtractor2013` or improved internal spectral flux) for offline beat tracking.
- **FR-003**: System MUST calculate and store multiple **Alternative BPM** candidates with associated confidence scores.
- **FR-004**: System MUST produce a full **Beat Grid** (timestamps of every detected beat) instead of a single average BPM.
- **FR-005**: System MUST record explicit flags for `ReinterpretedHalfTime` and `ReinterpretedDoubleTime` to provide traceability for the decision logic.

### Key Entities

- **BpmAnalysisResult**: The core data container including:
  - `PrimaryBpm` (double): The chosen musical tempo.
  - `AlternateBpms` (List<double>): Other likely candidates (harmonics).
  - `Confidence` (double): 0.0 to 1.0 score of the primary result.
  - `BeatTimesSeconds` (List<double>): Absolute timestamps of every beat.
  - `BeatIntervals` (List<double>): Time delta between consecutive beats.
  - `ReinterpretedFlags` (Boolean): Indicators for half/double time shifts.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: BPM accuracy on the `audiotest` reference dataset (including urban tracks) MUST reach at least **91%**.
- **SC-002**: 1.5x and 2:1 harmonic classification errors in Trap/Drill MUST be reduced by **90%** compared to the baseline.
- **SC-003**: The calculated beat grid MUST have a drift of less than **1ms** over a 5-minute track duration.
- **SC-004**: Data model MUST be fully serializable to JSON for persistence and UI binding.

## Assumptions

- Offline processing is prioritized over real-time speed to ensure maximum accuracy.
- We will leverage a specialized pre-processing band-filter to isolate rhythmic content (kicks/snares) before the final analysis pass.
- Standard 4/4 time signature is assumed unless the engine provides clear evidence otherwise.
