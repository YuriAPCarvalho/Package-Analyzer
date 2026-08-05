# Privacy Policy

Package-Analyzer is designed with a local-first approach.

The application does not automatically transmit source code, reports, project names, local paths, or scan results to external services.

Application data, including settings, history, and reports, remains stored on the user's computer.

By default, local data is stored at:

- `%LOCALAPPDATA%\TrivyProjectManager\data\trivy-project-manager.db`
- `%LOCALAPPDATA%\TrivyProjectManager\settings.json`
- `%LOCALAPPDATA%\TrivyProjectManager\Projects\<project-id>\reports\`
- `%LOCALAPPDATA%\TrivyProjectManager\Projects\<project-id>\logs\`

When the user enables in-project storage, reports and logs may be written to `.security/trivy/`.

## Internet access

The application may access the internet to:

- Check for and download official Package-Analyzer updates.
- Obtain or update the locally managed Trivy installation.
- Allow Trivy to update its public vulnerability databases.
- Restore dependencies from package registries configured by a trusted project during a full scan.
- Download Maven or Gradle distributions referenced by project-owned wrappers when those wrappers are executed.
- Query NVD, OSV, or GitHub Advisory for optional vulnerability enrichment when the user enables it.
- Open external references when explicitly requested by the user.

External enrichment through NVD, OSV, or GitHub Advisory is optional and disabled by default. When the user enables this feature, Package-Analyzer queries only public vulnerability identifiers such as CVE or GHSA. Source code, local paths, complete reports, and project names are not transmitted during these requests.

Full-scan preparation runs only after the user trusts a project. Package managers and build wrappers control which registries, mirrors, credentials, and download endpoints they contact; Package-Analyzer does not upload scan reports through this preparation flow.

If the user provides a GitHub Advisory token, it is stored locally in `%LOCALAPPDATA%\TrivyProjectManager\settings.json` and must never be committed to the repository.

## Secrets

Potential secrets found during scans are masked whenever Package-Analyzer processes them for display or storage.

## Telemetry

Package-Analyzer does not provide login functionality, collect telemetry or analytics, or automatically upload data.
