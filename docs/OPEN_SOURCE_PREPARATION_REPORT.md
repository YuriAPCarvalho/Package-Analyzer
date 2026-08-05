# Open-source Preparation Report

## Implemented

- Prepared the main repository to consolidate source code, documentation, releases, checksums, and Velopack artifacts.
- Removed dependencies on the `Package-Analyzer-Download` repository from the application, README, and release workflow.
- Added the MIT license, policies, templates, `CODEOWNERS`, SignPath checklist, and third-party notices.
- Updated the release workflow to publish to the main repository with `GITHUB_TOKEN`.
- Made the main repository public and confirmed that GitHub detects the MIT license.
- Confirmed during the final verification that the `Package-Analyzer-Download` repository could not be found.
- Corrected the release and CI YAML, then successfully validated and ran actionlint, the build, and the tests.
- Created public release `v0.1.1` from commit `eee351b2ea5a57e4325800a97029d2544e69c235`.
- Published six application artifacts with hashes verified against `SHA256SUMS.txt`.

## Security

- A static scan of the current state found no tracked sensitive files such as `.env` files, SQLite databases, logs, certificates, or keys.
- Static scans of the current state and repository history found no GitHub personal access tokens, private keys, or `Authorization: Bearer` headers with literal values.
- A broad search for terms such as `token`, `password`, `api_key`, and `secret` found only expected references in documentation, tests, masking code, the opt-in local GitHub Advisory configuration, and synthetic fixtures.
- The history contains old references to the workflow secret name (`PUBLIC_RELEASE_TOKEN`) and the `Package-Analyzer-Download` repository, but no literal token value.
- Gitleaks 8.30.1 was run temporarily, without a global installation, using:

```powershell
gitleaks detect --source . --redact --verbose
```

- Result: the tracked history was verified, and no leaks were found.
- The supplementary working-tree check with `gitleaks dir . --redact --verbose` scanned approximately 6.91 MB and also found no leaks.
- The secret fixture at `samples/trivy-reports/secret.json` uses a fake value and exists only to test masking.
- Real secret values must never be documented or displayed in logs.

## Open source

- License: MIT.
- Attribution preserved: `Package-Analyzer by: YuriAPCarvalho`.
- Added privacy, security, contribution, conduct, and code signing policies.
- Documented third-party components in `THIRD_PARTY_NOTICES.md`.

## Release

- Accepted tags: `vMAJOR.MINOR.PATCH`.
- Published tags are immutable and must not be moved or reused.
- Every tag requires a `## [MAJOR.MINOR.PATCH]` section in `CHANGELOG.md`.
- The workflow publishes artifacts to the GitHub release in the main repository.
- SHA-256 checksums are published in `SHA256SUMS.txt`.
- The single-repository model does not require `PUBLIC_RELEASE_TOKEN`.
- The remote `v0.1.0` tag points to `14d075a`; its runs failed in the legacy workflow, and no GitHub release was published.
- Release `v0.1.1` was published with provenance, an unsigned-artifact notice, and SHA-256 checksums.

## SignPath

- SignPath Foundation integration: planned / application pending.
- Manual tasks: enable Private Vulnerability Reporting, confirm 2FA, submit the application, configure the integration after approval, and manually approve every signing request.
- Limitation: binaries remain unsigned until approval is granted.

## Validated commands

The x64 `Microsoft.NETCore.App 9.0.18` runtime was installed to run the `net9.0` tests without roll-forward.

```powershell
dotnet restore TrivyProjectManager.sln -m:1 -nr:false
```

Result: successful.

```powershell
dotnet build TrivyProjectManager.sln --configuration Release --no-restore -m:1 -nr:false
```

Result: successful, with 0 warnings and 0 errors.

```powershell
dotnet test TrivyProjectManager.sln --configuration Release --no-build -m:1 -nr:false
```

Result: successful, with 33 tests passed (30 unit tests and 3 integration tests).

```powershell
actionlint
```

Result: actionlint 1.7.12 validated `.github/workflows/ci.yml` and `.github/workflows/release.yml` without diagnostics.

```powershell
gitleaks detect --source . --redact --verbose
```

Result: successful; the tracked history was verified, and no leaks were found.

```powershell
gitleaks dir . --redact --verbose
```

Result: successful; approximately 6.91 MB of the working tree was verified, and no leaks were found.

```powershell
git ls-files | rg -i "(^|/)(\.env|appsettings.*\.json)$|\.(pfx|p12|cer|key|pem|sqlite|sqlite3|db|db-shm|db-wal|log|dmp|dump)$|(^|/)(logs|reports|TestResults|\.security/trivy)(/|$)|Users/|C:/Users|C:\\Users"
```

Result: no tracked sensitive files were found.

```powershell
rg -n -i "github_pat_|ghp_|gho_|ghs_|ghu_|BEGIN PRIVATE KEY|Authorization:\s*Bearer" .
git grep -n -I -E "github_pat_|ghp_|gho_|ghs_|ghu_|BEGIN PRIVATE KEY|Authorization:[[:space:]]*Bearer" $(git rev-list --all)
```

Result: no GitHub personal access token, private key, or literal bearer token matched.

## Manual tasks

- Enable Private Vulnerability Reporting.
- Enable 2FA.
- Keep the `v0.1.1` tag immutable.
- Preserve unsigned-artifact notices until SignPath integration is active.
- Submit the SignPath Foundation application.
- Configure SignPath integration after approval.
- Manually approve every signing request.
