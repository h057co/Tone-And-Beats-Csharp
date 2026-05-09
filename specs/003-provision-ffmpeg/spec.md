# Feature Specification: FFmpeg Binary Provisioning

**Feature Branch**: `003-provision-ffmpeg`  
**Created**: 2026-05-09  
**Status**: Draft  
**Input**: User description: "Efectivamente el modulo de loudness muestra error."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Automated Dependency Resolution (Priority: P1)

As a user, when I open the application and FFmpeg is missing, I want the system to offer to download it or automatically resolve the dependency so that loudness analysis works without manual intervention.

**Why this priority**: Loudness analysis is a core feature of Tone & Beats v1.2.0. Without FFmpeg, the module is non-functional.

**Independent Test**: Remove the `ffmpeg/` directory from the application folder. Start the app. Verify that a download/setup process initiates and restores functionality.

**Acceptance Scenarios**:

1. **Given** FFmpeg is missing, **When** the application starts, **Then** a background task or setup wizard prompts to download the necessary binaries.
2. **Given** the download is complete, **When** I run a loudness analysis, **Then** results are displayed correctly without errors.

---

### User Story 2 - Build-Time Provisioning (Priority: P2)

As a developer, I want a script that can be run to fetch the correct FFmpeg binaries and place them in the expected `dependencies` folder, ensuring the build process always has what it needs.

**Why this priority**: Improves developer experience and prevents "it works on my machine" issues where FFmpeg is present only in certain environments.

**Independent Test**: Clone the repo without dependencies. Run the setup script. Verify that `dotnet build` now successfully bundles FFmpeg.

**Acceptance Scenarios**:

1. **Given** a clean repository, **When** `scripts/setup-ffmpeg.ps1` is run, **Then** `ffmpeg.exe` and `ffprobe.exe` are present in `../dependencies/ffmpeg/`.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a PowerShell script to download FFmpeg binaries from a reliable source (e.g., gyan.dev or official builds).
- **FR-002**: System MUST place binaries in the `dependencies/ffmpeg/` directory (relative to project root).
- **FR-003**: Application MUST detect the absence of FFmpeg on startup and notify the user with a "Resolve" action.
- **FR-004**: Download process MUST show progress and handle network failures gracefully.
- **FR-005**: Provisioned binaries MUST match the architecture (x64) and OS (Windows).

### Key Entities

- **FFmpeg Distribution**: Represents the external binary package (ZIP/7z) containing `ffmpeg.exe` and `ffprobe.exe`.
- **Environment State**: Represents the current availability and version of FFmpeg in the host system/application folder.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of users with an internet connection can restore loudness analysis functionality with a single click.
- **SC-002**: Setup script completes in under 60 seconds (excluding download time).
- **SC-003**: Provisioned binaries occupy less than 150MB of disk space.

## Assumptions

- We will use the LGPL or GPL builds of FFmpeg.
- User has permissions to write to the application directory.
- GitHub Actions or local builds will benefit from the setup script.
- [NEEDS CLARIFICATION: Should we download on-demand at runtime or just provide a developer script?]
