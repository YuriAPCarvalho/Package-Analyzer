# Changelog

## [Unreleased]

### Added

### Changed

### Fixed

### Security

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
