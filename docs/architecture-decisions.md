# Architecture Decisions

## Local-first

The application does not provide login functionality, collect telemetry or analytics, or automatically upload project content. Network access is limited to user-initiated or application-supporting operations: checking for Package-Analyzer updates, downloading the managed Trivy executable, allowing Trivy to update its public vulnerability databases, opening external references, and optionally enriching public vulnerability identifiers through NVD, OSV, or GitHub Advisory. External enrichment is disabled by default.

## Layered architecture

- `Domain` contains entities and enums without external dependencies.
- `Application` contains contracts, Trivy DTOs, the fault-tolerant parser, deduplication, counters, comparison logic, and validation.
- `Infrastructure` implements EF Core SQLite persistence, process execution, project detection, paths, retention, and Trivy integration.
- `App` contains the Avalonia UI, view models, dependency injection, dialogs, and local logging.

## Process execution

Commands are stored with the executable and arguments separated. Execution uses `ProcessStartInfo.ArgumentList`, `UseShellExecute=false`, asynchronous standard output and error streams, timeouts, and cancellation.

## Deduplication

The logical finding key uses:

```text
FindingType | VulnerabilityId or title/target | PackageName | InstalledVersion
```

Occurrences in different targets are stored as `FindingOccurrence` records so they do not inflate the main summary.

## SQLite

The database is stored under `%LOCALAPPDATA%\TrivyProjectManager\data\`. Reports and logs may use the configured central storage or `.security/trivy/` within the project.

## Migrations

The initial migration is maintained in the infrastructure project. Because it was created manually for this MVP, the application suppresses only the pending-model-changes warning associated with the manually maintained snapshot.
