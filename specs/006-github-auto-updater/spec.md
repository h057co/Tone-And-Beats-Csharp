# Feature Specification: GitHub Auto-Updater

**Feature Branch**: `006-github-auto-updater`  
**Created**: 2026-05-09  
**Status**: Draft  
**Input**: User description: "implementar un Auto Updater que se conecte haciendo request de las versiones publicadas en github"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Automatic Update Check (Priority: P1)

As a user, I want the application to automatically check for new versions on startup so that I am always aware of improvements and bug fixes without manual effort.

**Why this priority**: Essential for keeping the user base on the latest version and reducing support for old bugs.

**Independent Test**: Can be tested by launching the app with a lower version number than the latest GitHub release and verifying the check occurs.

**Acceptance Scenarios**:

1. **Given** a new version is available on GitHub, **When** the app starts, **Then** a notification or status indicator should appear.
2. **Given** the app is already on the latest version, **When** it starts, **Then** no update prompt should be shown.

---

### User Story 2 - Manual Update Trigger (Priority: P2)

As a user, I want a "Check for Updates" button in the About or Settings section so that I can manually verify if a new version exists at any time.

**Why this priority**: Provides user control and a way to re-check if the automatic check was missed or failed.

**Independent Test**: Clicking the button triggers a specific API request and displays results.

**Acceptance Scenarios**:

1. **Given** internet connectivity, **When** clicking "Check for Updates", **Then** the system returns either "You are up to date" or "New version available".

---

### User Story 3 - Download and Install Guidance (Priority: P1)

As a user, I want a clear path to download the new version so that I can install it easily.

**Why this priority**: An update notification is useless without an easy way to act on it.

**Independent Test**: Clicking the "Update Now" button opens the relevant download link or starts the download process.

**Acceptance Scenarios**:

1. **Given** a new version is detected, **When** clicking "Download", **Then** the user is directed to the latest GitHub release page or the installer download starts.

---

### Edge Cases

- **Offline / No Connection**: What happens when the GitHub API is unreachable? System MUST fail silently without annoying the user with error popups on every startup.
- **GitHub Rate Limiting**: How does the system handle API rate limits?
- **Corrupt Version Metadata**: How to handle unexpected JSON formats from GitHub?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST query the GitHub Releases API (`repos/HostilityMusic/ToneAndBeats/releases/latest`).
- **FR-002**: System MUST compare the current assembly version with the GitHub `tag_name` (using Semantic Versioning logic).
- **FR-003**: System MUST NOT block the main UI thread during the update check (MUST be asynchronous).
- **FR-004**: System MUST perform a silent update by downloading the update in the background and prompting for a restart to apply changes.
- **FR-005**: System MUST trigger the update check manually via a "Check for Updates" button located in the About section.
- **FR-006**: System MUST persist a "Skip this version" preference if the user declines an update.

### Key Entities *(include if feature involves data)*

- **GitHub Release**: Represents the metadata from GitHub (version, changelog, download URL).
- **Version Preference**: Local configuration storing the last checked version and "skip" flags.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The update check (API call + comparison) MUST complete in under 3 seconds on a standard broadband connection.
- **SC-002**: 100% of detected updates MUST display the correct version number and a direct link to the changelog.
- **SC-003**: ZERO crashes or UI freezes during network failure or API timeouts.

## Assumptions

- **Repository Access**: The GitHub repository is public or the app has a way to access the release API without sensitive tokens.
- **Version Format**: GitHub tags follow `vX.Y.Z` or `X.Y.Z` format consistently.
- **Internet Dependency**: Users require internet access for this feature to function.
