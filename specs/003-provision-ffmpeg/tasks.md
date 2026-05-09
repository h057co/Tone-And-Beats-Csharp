# Tasks: FFmpeg Binary Provisioning

**Input**: Design documents from `specs/003-provision-ffmpeg/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Tests are OPTIONAL - only implementation tasks are included below as TDD was not explicitly requested.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [x] T001 Create `scripts/setup-ffmpeg.ps1` to automate developer environment setup from repo root

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T002 [P] Implement `IDependencyService` interface in `Interfaces/IDependencyService.cs`
- [ ] T003 [P] Create `DependencyInfo` model in `Models/DependencyInfo.cs` per data-model.md
- [ ] T004 Update `LoudnessAnalyzer.cs` to utilize `IDependencyService` for binary path resolution

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Automated Dependency Resolution (Priority: P1) 🎯 MVP

**Goal**: Enable the application to download missing FFmpeg binaries at runtime.

**Independent Test**: Delete the `ffmpeg` folder and verify that the application detects the absence and provides a resolution path.

### Implementation for User Story 1

- [ ] T005 [US1] Implement `DependencyService` in `Services/DependencyService.cs` with ZIP extraction logic
- [ ] T006 [US1] Add `IsDownloading` and `DownloadProgress` properties to `ViewModels/MainViewModel.cs`
- [ ] T007 [US1] Implement `CheckDependenciesCommand` and `ResolveDependenciesCommand` in `ViewModels/MainViewModel.cs`
- [ ] T008 [US1] Add `DependencyOverlay` UI component to `MainWindow.xaml` following Digital Brutalist guidelines (1px border, 0px radius)
- [ ] T009 [US1] Integrate `DependencyService` with `MainViewModel` startup sequence

**Checkpoint**: User Story 1 functional - FFmpeg can be downloaded and installed at runtime.

---

## Phase 4: User Story 2 - Build-Time Provisioning (Priority: P2)

**Goal**: Ensure developers have a standardized way to fetch binaries.

**Independent Test**: Run `.\scripts\setup-ffmpeg.ps1` in a clean environment and verify `dependencies/ffmpeg/` is populated.

### Implementation for User Story 2

- [ ] T010 [US2] Enhance `scripts/setup-ffmpeg.ps1` with MD5 verification and multi-source fallbacks
- [ ] T011 [US2] Update `AudioAnalyzer.csproj` build targets to warn if `setup-ffmpeg.ps1` hasn't been run in a developer environment

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Final verification and consistency checks.

- [ ] T012 [P] Verify UI responsiveness during active downloads in `MainWindow.xaml`
- [ ] T013 Update `walkthrough.md` with a recording of the automated download process
- [ ] T014 Run `quickstart.md` validation steps

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Can start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1 completion. BLOCKS all user stories.
- **User Stories (Phase 3+)**: Depend on Foundational phase completion. US1 (Runtime) and US2 (Build-time) can proceed in parallel.
- **Polish (Phase 5)**: Depends on all user stories being complete.

### Parallel Opportunities

- T002 and T003 can be implemented in parallel.
- US1 UI (T008) can be designed in parallel with US1 Service logic (T005) if using mock data.

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 & 2.
2. Complete User Story 1 (Runtime resolution).
3. **STOP and VALIDATE**: Verify that a user without FFmpeg can now use the app by clicking "Resolve".
