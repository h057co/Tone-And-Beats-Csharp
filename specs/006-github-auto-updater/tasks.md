# Tasks: GitHub Auto-Updater

**Input**: Design documents from `/specs/006-github-auto-updater/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, quickstart.md

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and repository configuration

- [x] T001 [P] Configure GitHub repository and push initial code using `gh repo create`
- [x] T002 [P] Add `.github/workflows/release.yml` for automated release packaging
- [x] T003 [P] Update `AudioAnalyzer.csproj` version to 1.2.0 as the current baseline

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure for the update system

- [x] T004 [P] Create `IUpdateService` interface in `Interfaces/IUpdateService.cs`
- [x] T005 [P] Create `UpdateInfo` model in `Models/UpdateInfo.cs`
- [x] T006 Implement base `UpdateService` with GitHub API client in `Services/UpdateService.cs`

**Checkpoint**: Infrastructure ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Automatic Update Check (Priority: P1) 🎯 MVP

**Goal**: Automatically check for new versions on startup without user intervention.

**Independent Test**: App logs "New version available" on startup when a higher version exists on GitHub.

### Implementation for User Story 1

- [x] T007 [P] [US1] Implement semantic version comparison logic in `Services/UpdateService.cs`
- [x] T008 [US1] Add background update check on application startup in `App.xaml.cs`
- [x] T009 [US1] Add error handling for offline mode/timeouts in `UpdateService.cs`

**Checkpoint**: User Story 1 functional (background check working)

---

## Phase 4: User Story 3 - Download and Install (Priority: P1)

**Goal**: Provide a clear, automated path to download and apply the update.

**Independent Test**: App downloads the new binary, restarts, and opens the updated version.

### Implementation for User Story 3

- [x] T010 [P] [US3] Implement `DownloadUpdateAsync` with progress reporting in `Services/UpdateService.cs`
- [x] T011 [US3] Implement `ApplyUpdateAndRestart` with PowerShell bridge in `Services/UpdateService.cs`
- [x] T012 [US3] Add progress bar and status UI to `AboutWindow.xaml`

**Checkpoint**: User Story 3 functional (update application working)

---

## Phase 5: User Story 2 - Manual Update Trigger (Priority: P2)

**Goal**: Allow users to manually re-check for updates from the UI.

**Independent Test**: Clicking "BUSCAR" in the About window triggers a fresh API check.

### Implementation for User Story 2

- [x] T013 [P] [US2] Design "Actualizaciones" section in `AboutWindow.xaml` with Brutalist theme
- [x] T014 [US2] Implement button click handlers in `AboutWindow.xaml.cs`
- [x] T015 [US2] Integrate manual check with `IUpdateService` in `AboutWindow.xaml.cs`

**Checkpoint**: User Story 2 functional (manual trigger working)

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements and edge case handling

- [x] T016 [P] Implement version skip persistence in `Services/StorageService.cs`
- [x] T017 Final UI polish for Digital Brutalist theme (borders, font consistency)
- [x] T018 Verify entire update flow using `quickstart.md` validation steps

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Can start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1 completion.
- **User Stories (Phase 3+)**: Depend on Foundational (Phase 2) completion.
- **Polish (Phase 6)**: Depends on all user stories being complete.

### User Story Dependencies

- **User Story 1 (P1)**: Independent after Phase 2.
- **User Story 3 (P1)**: Logically follows US1/US2 detection.
- **User Story 2 (P2)**: Independent after Phase 2.

---

## Parallel Opportunities

- Phase 1 tasks (T001-T003) can run in parallel.
- Phase 2 tasks (T004-T005) can run in parallel.
- US1 (T007) and US3 (T010) and US2 (T013) can start in parallel once infrastructure is ready.

---

## Implementation Strategy

### MVP First (User Story 1 & 3)
1. Setup infrastructure.
2. Implement US1 (Detection).
3. Implement US3 (Application).
4. Validate auto-update works end-to-end.

### Incremental Delivery
1. Foundation -> Detection -> Application -> Manual Trigger -> Polish.
