# Tasks: Key Detection & UI Refinement

**Input**: Design documents from `/specs/001-key-detection-optimization/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)
- [x] T001 Verify project environment (NAudio, FFMpegCore) in `AudioAnalyzer.csproj`
- [x] T002 [P] Configure `KeyDetectorTest.csproj` for .NET 8.0-windows and set StartupObject

## Phase 2: Foundational (Blocking Prerequisites)
- [x] T003 Define `KeyDetectionResult` DTO and `IKeyDetector` interface in `Services/IKeyDetector.cs`
- [x] T004 Setup basic `KeyDetector` class skeleton in `Services/KeyDetector.cs`
- [x] T005 [P] Create initial `KeyDetectorTest.cs` with basic chord generation utilities

## Phase 3: User Story 1 - Analyze Non-Standard Audio (Priority: P1)
**Goal**: Detect musical key correctly even when audio is detuned (e.g., 432Hz).

- [x] T006 [P] [US1] Implement `DetectTuningOffset` logic in `Services/KeyDetector.cs`
- [x] T007 [US1] Integrate tuning offset into `ComputePitchClassProfile` frequency mapping in `Services/KeyDetector.cs`
- [x] T008 [US1] Add Gaussian soft-assignment kernel for "fuzzy" pitch analysis in `Services/KeyDetector.cs`
- [x] T009 [US1] Implement Krumhansl-Schmuckler profile correlation in `Services/KeyDetector.cs`
- [x] T010 [US1] Add 432Hz validation cases to `scratch/KeyDetectorTest.cs`

## Phase 4: User Story 2 - Keyboard Visuals & Interaction (Priority: P1)
**Goal**: Chromatic piano keyboard with key highlighting and Primary/Alternative swap.

**Independent Test**: Open overlay, verify black keys exist, click "Detected Key" label, and see piano highlights swap.

- [ ] T011 [P] [US2] Update `MainWindow.xaml` piano layout to a 12-note chromatic octave (7 white, 5 black keys)
- [ ] T012 [P] [US2] Implement `KeyToPianoIndexConverter` to map note names to piano key indices in `MainWindow.xaml`
- [ ] T013 [US2] Implement `SwapKeyCommand` in `ViewModels/MainViewModel.cs` to toggle primary/alternative results
- [ ] T014 [US2] Add visual "Press Effect" to piano keys using `Storyboard` in `MainWindow.xaml`
- [ ] T015 [US2] Bind piano key `Background` to detected scale degree highlighting logic

## Phase 5: User Story 4 - Reliable Loudness Reporting (Priority: P1)
**Goal**: Ensure LUFS/LRA/Peak results are always visible and accurate.

**Independent Test**: Load track and see non-zero values for all loudness metrics (no "--" or empty fields).

- [ ] T016 [P] [US4] Add `Status` and `Error` properties to `Models/LoudnessResult.cs`
- [ ] T017 [US4] Refactor FFmpeg output parsing in `Services/LoudnessAnalyzer.cs` to use robust regex scanners
- [ ] T018 [US4] Update `MainWindow.xaml` to display "ERROR" or "N/A" with appropriate styling on parsing failure

## Phase 6: User Story 3 - BPM-Synced Tone Playback (Priority: P2)
**Goal**: Pulsing sine wave tone synchronized with detected BPM.

**Independent Test**: Enable "Play Scale" and verify the tone pulses at the track's tempo.

- [ ] T019 [P] [US3] Create `ToneGeneratorService.cs` using `NAudio` with amplitude gating capability
- [ ] T020 [US3] Implement `PulseTimer` logic in `ViewModels/MainViewModel.cs` synchronized with the track's BPM
- [ ] T021 [US3] Integrate `ToneGeneratorService` into `PlayScaleCommand` with 50ms decay envelope

## Phase 7: Polish & Cross-Cutting Concerns
- [x] T022 Remove temporary debug logs
- [ ] T023 [P] Update `walkthrough.md` with recordings of the updated Keyboard and Loudness view
- [x] T024 Fix header and footer layout overlaps in `MainWindow.xaml`
- [ ] T025 Run full accuracy benchmark for SC-001 (+/- 5 cents) in `AudioAnalyzer.PerfTest`

## Dependencies & Execution Order
1. **Loudness Fixes (Phase 5)**: Critical for baseline data visibility.
2. **Keyboard Interaction (Phase 4)**: Essential for user verification.
3. **BPM Tone (Phase 6)**: Advanced verification feature.

## Implementation Strategy
- **MVP**: Phases 1-4 provide a complete, interactive key detection and verification experience.
- **Incremental**: Phase 5-6 add professional mastering metrics and auditory verification.
