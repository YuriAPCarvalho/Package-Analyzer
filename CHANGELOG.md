# Changelog

## [Unreleased]

### Added

### Changed

### Fixed

### Security

## [0.2.0] - 2026-08-05

### Added

- Automatic multi-target detection and preparation for .NET, npm, pnpm, Yarn, Maven, and Gradle projects.
- Support for mixed repositories, Node workspaces, Maven aggregators, Gradle multi-project builds, and project-owned build wrappers.
- Per-project automatic/manual preparation mode, persisted project trust, and a `Completed with warnings` scan status.

### Changed

- Full scans redetect project targets before each run and execute restore/install/build with the correct working directory and explicit manifest.
- Preparation failures are isolated by target; available targets and Trivy continue so findings can still be persisted.
- Partial scan results participate in the dashboard, history, and scan comparison.

### Fixed

- Fixed the SQLite/EF Core failure when filtering security exceptions by `DateTimeOffset` expiration.
- Fixed `.NET` restore/build failures caused by running without an explicit solution or project file.
- Added safe execution support for Windows package-manager shims and Maven/Gradle batch wrappers.

### Security

- Full-scan preparation now requires explicit per-project trust and provides a revocation control.
- Missing SDKs, runtimes, and build tools are detected per target and reported without installing system toolchains.

## [0.1.1] - 2026-08-05

### Added

- MIT license and open-source governance documentation.
- Privacy, security, contribution, code signing, third-party notice, and SignPath checklist documents.
- GitHub issue templates, pull request template, and CODEOWNERS.
- Continuous integration for workflow validation, build, and tests.

### Changed

- Release distribution uses the main `Package-Analyzer` repository instead of a separate download repository.
- Release tags are immutable, and `v0.1.1` is the first public release built from the single-repository workflow.

### Fixed

- Corrected the release workflow YAML so tagged builds can start normally.

### Security

- Expanded repository ignore rules for local data, reports, logs, environment files, and signing material.
- Validated the repository history with Gitleaks 8.30.1 without finding leaks.

## [0.1.0] - 2026-08-05

- Preparation tag created before the migration to the single-repository model.
- Artifact publishing failed in the legacy workflow; no GitHub release was published for this tag.
