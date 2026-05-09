---

description: "Task list for BPM Accuracy & Urban Strategy implementation"
---

# Tasks: BPM Accuracy & Urban Strategy Optimization

**Input**: Design documents from `/specs/004-bpm-accuracy-optimization/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md

**Tests**: Tests are performed via the `AudioAnalyzer.PerfTest` console application using the `audiotest` dataset.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [x] T001 Provision `essentia_streaming_extractor_music.exe` binaries in `Services/BinaryProvisioningService.cs`
- [x] T002 [P] Create `BpmAnalysisResult` model in `Models/BpmAnalysisResult.cs` per data-model.md
- [x] T003 [P] Add JSON serialization support for `BpmAnalysisResult` in `Services/StorageService.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure for MIR analysis

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T004 Implement `EssentiaWrapper` in `Services/EssentiaWrapper.cs` to execute CLI and parse JSON output
- [x] T005 [P] Implement `AudioPreprocessor` in `Services/AudioPreprocessor.cs` for 44.1kHz resampling and normalization
- [x] T006 Update `BpmDetector.cs` constructor to accept new analysis dependencies

**Checkpoint**: Foundation ready - MIR engine is functional and reachable from C#.

---

## Phase 3: User Story 1 - High-Precision Urban Tempo (Priority: P1) 🎯 MVP

**Goal**: Implement the Urban Strategy (half-time/double-time resolution) for Trap and Reggaetón.

**Independent Test**: Run `PerfTest` and verify that Trap tracks (140 BPM technical) are correctly identified as 70 BPM musical.

### Implementation for User Story 1

- [x] T007 [P] [US1] Implement `UrbanStrategyHeuristic` in `Services/UrbanStrategyHeuristic.cs` (Snare/Hat density analysis)
- [x] T008 [US1] Integrate `EssentiaWrapper` as the primary detection source in `BpmDetector.cs`
- [x] T009 [US1] Implement "Harmonic Guard" logic in `BpmDetector.cs` using the heuristic from T007
- [x] T010 [US1] Update `AudioAnalyzer.PerfTest/Program.cs` to include detailed harmonic error reporting and "Reinterpreted" status

**Checkpoint**: User Story 1 complete. Overall accuracy should now reach >91% on the reference dataset.

---

## Phase 4: User Story 2 - Advanced Rhythmic Data (Priority: P2)

**Goal**: Expose full beat grids and enable manual BPM swapping in the UI.

**Independent Test**: Load a track and verify that the "BPM Swap" button correctly toggles between primary and alternative candidates.

### Implementation for User Story 2

- [x] T011 [P] [US2] Update `BpmDetector.cs` to return the complete `BpmAnalysisResult` object
- [x] T012 [P] [US2] Update `MainViewModel.cs` to store the full `BpmAnalysisResult` and its alternative candidates
- [x] T013 [US2] Implement `SwapBpmCommand` in `ViewModels/MainViewModel.cs` to switch Primary and Alternative BPMs
- [x] T014 [US2] Integrate `BeatTimesSeconds` into the waveform rendering logic in `Views/WaveformView.xaml.cs` (Optional/Polish)

**Checkpoint**: User Story 2 complete. User can now interact with alternative results and see the beat grid.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: UI refinement and final validation

- [x] T015 [P] Add "BPM Swap" toggle button in `Views/MainWindow.xaml` using Digital Brutalist styling (Consolas, 1px border)
- [x] T016 [P] Add micro-animation for the BPM display update in `Views/MainWindow.xaml`
- [x] T017 Perform final verification of all success criteria (SC-001 to SC-004) using `PerfTest`
- [x] T018 [P] Update documentation in `README.md` to reflect new high-precision mode requirements

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Can start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1 completion.
- **User Story 1 (P1)**: Depends on Phase 2. **CRITICAL for MVP**.
- **User Story 2 (P2)**: Depends on Phase 3 completion for the actual alternative data.
- **Polish (Final Phase)**: Depends on all user stories.

### Parallel Opportunities

- T002 and T003 can run in parallel.
- T005 can run in parallel with T004.
- T015 and T016 (UI) can run in parallel with final verification (T017).

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 & 2 (MIR Foundation).
2. Complete Phase 3 (Urban Strategy).
3. **STOP and VALIDATE**: Run `PerfTest`. If accuracy is 91%, the core value is delivered.

### Incremental Delivery

1. MIR engine functional → Foundation.
2. Accuracy improved → MVP ready.
3. UI features added → Final polish.
