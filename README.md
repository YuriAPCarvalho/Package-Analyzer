# Package-Analyzer

**Author:** YuriAPCarvalho

**SPDX-License-Identifier:** MIT

[![Release](https://github.com/YuriAPCarvalho/Package-Analyzer/actions/workflows/release.yml/badge.svg)](https://github.com/YuriAPCarvalho/Package-Analyzer/actions/workflows/release.yml)
[![GitHub release](https://img.shields.io/github/v/release/YuriAPCarvalho/Package-Analyzer)](https://github.com/YuriAPCarvalho/Package-Analyzer/releases/latest)
[![License: MIT](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Windows](https://img.shields.io/badge/platform-Windows-0078d4.svg)](#installation)
[![.NET](https://img.shields.io/badge/.NET-9.0-512bd4.svg)](#development)
[![Open source](https://img.shields.io/badge/status-open%20source-brightgreen.svg)](CONTRIBUTING.md)

**Package-Analyzer** is a desktop application for running local security scans on software projects. It automates the detection of known vulnerabilities, misconfigurations, and exposed secrets, then organizes the findings to help users address the identified issues.

The application uses **Trivy** as one of its scanning engines. Trivy is an open-source project maintained by **Aqua Security**.

> **Notice:** Package-Analyzer is an independent open-source project and is not affiliated with, partnered with, or endorsed by Aqua Security.

---

## Features

- Register and manage local projects.
- Automatically detect .NET, npm, pnpm, Yarn, Maven, and Gradle projects, including mixed repositories and monorepos.
- Run quick scans with a locally installed Trivy executable.
- Run full scans that automatically restore or install dependencies and build every detected target before scanning.
- Keep Trivy results when a preparation target fails, marking the scan as completed with warnings.
- View a dashboard with:
  - Vulnerabilities by severity.
  - Unique vulnerabilities.
  - Misconfigurations.
  - Detected secrets.
- Review vulnerability details for CVE and GHSA findings, including:
  - Affected package.
  - Installed version.
  - Fixed version.
  - Official references.
- Automatically group equivalent occurrences.
- Compare scans and review scan history.
- Classify vulnerabilities as:
  - New.
  - Existing.
  - Regressed.
  - Resolved.
- Review misconfigurations and secrets in dedicated tabs.
- Automatically mask secrets before displaying or storing them.
- Store application data in a local SQLite database.
- Check for and install application updates through Velopack.
- Use an official installer for Windows x64.

---

## Screenshots

Public application screenshots should be stored in:

```text
docs/images/
```

Replace this note with the corresponding images when screenshots become available.

---

## Installation

Package-Analyzer is currently distributed for **Windows x64**.

### Steps

1. Open the official releases page:

   https://github.com/YuriAPCarvalho/Package-Analyzer/releases/latest

2. Download:

   ```text
   YuriAPCarvalho.PackageAnalyzer-stable-Setup.exe
   ```

3. Verify its SHA-256 hash against the value in `SHA256SUMS.txt`.

4. Run the installer.

---

## Security notice

The application is **not yet digitally signed**.

Windows SmartScreen may display a security warning during installation or the first launch.

To verify the file's authenticity:

- Download it only from the official GitHub repository.
- Verify the SHA-256 hash published with each release.

---

## Locally stored data

SQLite database:

```text
%LOCALAPPDATA%\TrivyProjectManager\data\trivy-project-manager.db
```

Settings:

```text
%LOCALAPPDATA%\TrivyProjectManager\settings.json
```

Reports:

```text
%LOCALAPPDATA%\TrivyProjectManager\Projects\<project-id>\reports\
```

Logs:

```text
%LOCALAPPDATA%\TrivyProjectManager\Projects\<project-id>\logs\
```

Optional in-project storage:

```text
.security/trivy/
```

Uninstalling removes only the application. Data under `%LOCALAPPDATA%\TrivyProjectManager` may remain on the computer to preserve scan history and user settings.

---

## Privacy

Package-Analyzer:

- Does not require registration.
- Does not require authentication.
- Does not collect telemetry.
- Does not automatically transmit source code.
- Does not transmit project names.
- Does not transmit local paths.
- Does not transmit reports.
- Does not transmit scan results.

The application accesses the internet only when needed to:

- Check for new Package-Analyzer versions.
- Download or update the application-managed Trivy installation.
- Allow Trivy to update its public vulnerability databases.
- Restore project dependencies from configured NuGet, npm, Maven, and Gradle registries during a trusted full scan.
- Download the Maven or Gradle distribution declared by a project-owned wrapper when that wrapper is executed.
- Query NVD, OSV, or GitHub Advisory for optional vulnerability enrichment when the user enables it.
- Open external references requested by the user.

External enrichment through NVD, OSV, or GitHub Advisory is optional and disabled by default.

When enrichment is enabled, the application queries only public identifiers such as **CVE** and **GHSA**. It does not transmit content from the scanned project.

For more information, see [PRIVACY.md](PRIVACY.md).

---

## Development

### Requirements

- .NET SDK 9.0
- Windows
- A local Trivy installation or automatic installation enabled

### Common commands

```powershell
dotnet restore TrivyProjectManager.sln -m:1 -nr:false

dotnet build TrivyProjectManager.sln --configuration Release --no-restore -m:1 -nr:false

dotnet test TrivyProjectManager.sln --configuration Release --no-build -m:1 -nr:false

dotnet run --project src\TrivyProjectManager.App\TrivyProjectManager.App.csproj
```

### Project structure

```text
src/
├── TrivyProjectManager.App/
├── TrivyProjectManager.Application/
├── TrivyProjectManager.Domain/
└── TrivyProjectManager.Infrastructure/

tests/
├── TrivyProjectManager.UnitTests/
└── TrivyProjectManager.IntegrationTests/

samples/
└── trivy-reports/
```

The application automatically looks for `trivy.exe` in the system `PATH` or at the path configured by the user.

Quick scans run Trivy without executing project commands. Full scans detect .NET, Node.js, and Java targets again before each run. The first full scan asks the user to trust the project because package installation and build commands can execute scripts supplied by that project. Trust can be revoked in project settings.

Package-Analyzer does not install the .NET SDK, Node.js, or a JDK. Missing toolchains are reported per target; available targets and Trivy continue to run. Maven and Gradle wrappers committed to the project are preferred over global installations.

When automatic installation is enabled, Trivy is downloaded to:

```text
%LOCALAPPDATA%\TrivyProjectManager\tools\trivy\trivy.exe
```

---

## Publishing releases

Official releases are available at:

https://github.com/YuriAPCarvalho/Package-Analyzer/releases

To create a release:

```powershell
$tag = "v0.2.0"

git tag $tag

git push origin $tag
```

Every release tag must follow this pattern:

```text
vMAJOR.MINOR.PATCH
```

It must also have a corresponding entry in `CHANGELOG.md`.

Example:

```md
## [0.2.0]
```

Release tags are immutable and must never be moved or reused.

The `v0.1.0` tag is preserved only as a historical record of an earlier publishing attempt.

The automated workflow at:

```text
.github/workflows/release.yml
```

performs the following tasks:

- Restores dependencies.
- Builds the application.
- Runs the tests.
- Publishes the final release.
- Creates Velopack packages.
- Generates `SHA256SUMS.txt`.
- Publishes the artifacts to the GitHub releases page.

---

## Code signing

Integration with the **SignPath Foundation** is planned but is still awaiting approval.

For more information, see [CODE_SIGNING_POLICY.md](CODE_SIGNING_POLICY.md).

---

## Documentation

- [Privacy Policy](PRIVACY.md)
- [Security Policy](SECURITY.md)
- [Contributing Guide](CONTRIBUTING.md)
- [Code of Conduct](CODE_OF_CONDUCT.md)
- [Code Signing Policy](CODE_SIGNING_POLICY.md)
- [Third-party Notices](THIRD_PARTY_NOTICES.md)
- [License](LICENSE)
- [SignPath Foundation Application Checklist](docs/SIGNPATH_APPLICATION_CHECKLIST.md)
- [Open-source Preparation Report](docs/OPEN_SOURCE_PREPARATION_REPORT.md)
