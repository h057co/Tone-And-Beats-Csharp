# Tasks: FFmpeg Verification & Keyboard Fixes

**Input**: Design documents from `specs/002-fix-ffmpeg-keyboard/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Tests are OPTIONAL - only implementation tasks are included below as TDD was not explicitly requested.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [x] T001 Update plan reference in AGENTS.md to point to specs/002-fix-ffmpeg-keyboard/plan.md
- [x] T002 [P] Create initial ScaleMap entity in Models/ScaleMap.cs per data-model.md

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T003 Update LoudnessResult model with HasError and ErrorMessage properties in Models/LoudnessResult.cs
- [x] T004 Update IToneGeneratorService contract with scale playback methods in Interfaces/IToneGeneratorService.cs
- [x] T005 [P] Implement ScaleMap logic for major/minor intervals in Models/ScaleMap.cs

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Verify FFmpeg Availability (Priority: P1) 🎯 MVP

**Goal**: Ensure FFmpeg is available and loudness analysis parsing is robust.

**Independent Test**: Delete the `ffmpeg` folder and verify the UI reports "FFmpeg Missing" in the loudness section.

### Implementation for User Story 1

- [x] T006 Implement FFmpeg binary existence check at startup in Services/LoudnessAnalyzer.cs
- [x] T007 Implement Regex-based extraction for FFnorm summary parsing in Services/LoudnessAnalyzer.cs
- [x] T008 [P] Update MainViewModel to propagate loudness analysis errors to the View state in ViewModels/MainViewModel.cs
- [x] T009 Update MainWindow.xaml to show error ToolTips for the Loudness indicator in Views/MainWindow.xaml

**Checkpoint**: User Story 1 functional - FFmpeg module verification and parsing is stable.

---

## Phase 4: User Story 2 - Keyboard Piano Octave (Priority: P1)

**Goal**: Professional piano visualization with scale-based highlighting.

**Independent Test**: Load a C Major track and verify that C, D, E, F, G, A, B keys are highlighted in the overlay.

### Implementation for User Story 2

- [x] T010 [P] [US2] Create ScaleNoteHighlightConverter for key styling in Infrastructure/ScaleNoteHighlightConverter.cs
- [x] T011 [US2] Implement 12-note chromatic piano layout using Grid/UniformGrid in Views/MainWindow.xaml
- [x] T012 [US2] Bind piano key highlights to the ScaleMap in ViewModels/MainViewModel.cs
- [x] T013 [US2] Apply Digital Brutalist styling (0px radius, 1px border) to all piano keys in Views/MainWindow.xaml

**Checkpoint**: User Story 2 functional - Piano visualization reflects the detected musical key.

---

## Phase 5: User Story 3 - Tone Generator Scale Playback (Priority: P1)

**Goal**: Rhythmic audio preview of the detected scale.

**Independent Test**: Enable "Play Scale" and verify tones follow the BPM flash rhythm.

### Implementation for User Story 3

- [x] T014 [US3] Implement GatedSampleProvider with linear decay envelope in Services/ToneGeneratorService.cs
- [x] T015 [US3] Integrate scale sequence playback triggered by the BPM clock in Services/ToneGeneratorService.cs
- [x] T016 [US3] Add ToggleScalePlayback command and UI controls in ViewModels/MainViewModel.cs
- [x] T017 [US3] Ensure tone generator disposal on application exit in ViewModels/MainViewModel.cs

**Checkpoint**: User Story 3 functional - Audio feedback matches detected key and BPM.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final verification and consistency checks.

- [x] T018 [P] Verify contrast for piano highlights across all visual themes in Themes/BrutalistTheme.xaml and Themes/DarkTheme.xaml
- [x] T019 Update walkthrough.md with screenshots of the new keyboard overlay
- [x] T020 Run AudioAnalyzer.PerfTest to ensure low-latency audio-visual sync

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Phase 1. Blocks all User Stories.
- **User Stories (Phase 3-5)**: Can be implemented in parallel after Phase 2 is complete.
- **Polish (Phase 6)**: Depends on all user stories being complete.

### Parallel Opportunities

- T002, T005, T010 can be worked on independently.
- User Stories 1 and 2 are largely decoupled and can be implemented simultaneously.

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 & 2.
2. Complete User Story 1 (FFmpeg Verification).
3. **STOP and VALIDATE**: Verify that loudness results are stable.
