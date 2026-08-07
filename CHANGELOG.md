# Changelog

## [Unreleased]

### Added

### Changed

### Fixed

### Security

## [0.3.0] - 2026-08-07

### Added

- Copy and TXT export actions for current scan logs, misconfiguration findings, and secret findings.
- Semantic log coloring for informational, warning, and error output.
- Additional case-insensitive Dockerfile detection for filenames containing `Dockerfile` with prefixes or suffixes.

### Changed

- On-screen scan logs now retain structured stream and severity information for display and export.
- Text reports use a consistent UTF-8 format with project, category, totals, and detailed findings.

### Fixed

- Dockerfiles such as `Dockerfile.dev`, `Dockerfile-hom`, and `api-Dockerfile` are now included in misconfiguration scans.

### Security

- Secret reports and clipboard exports contain only the masked snippets already approved for display and persistence.

## [0.2.1] - 2026-08-05

### Added

- Automated startup checks for the latest stable Trivy release, with offline fallback to an existing usable installation.

### Changed

- Trivy is now checked on every startup and migrated to an application-managed installation without overwriting external copies.

### Fixed

- Process output is decoded and persisted as UTF-8, with terminal ANSI sequences removed before display.
- Windows package-manager commands now prefer `.cmd` launchers such as `npm.cmd` over extensionless Unix scripts.

### Security

- Managed Trivy downloads are validated against the release asset SHA-256 digest before atomic installation.

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
