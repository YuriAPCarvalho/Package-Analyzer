# Contributing

Thank you for considering a contribution to Package-Analyzer.

## Issues

Use issues to report bugs, propose improvements, or start technical discussions. Before opening an issue, check whether a related discussion already exists.

Include only sanitized logs, screenshots, and examples. Never include tokens, passwords, private keys, customer data, sensitive paths, or reports containing real secrets.

## Branches

Use short, descriptive branch names:

- `feat/<description>`
- `fix/<description>`
- `docs/<description>`
- `chore/<description>`

## Commits

Suggested prefixes:

- `feat:`
- `fix:`
- `docs:`
- `refactor:`
- `test:`
- `build:`
- `ci:`
- `chore:`

## Local development

```powershell
dotnet restore TrivyProjectManager.sln -m:1 -nr:false
dotnet build TrivyProjectManager.sln --configuration Release --no-restore -m:1 -nr:false
dotnet test TrivyProjectManager.sln --configuration Release --no-build -m:1 -nr:false
```

## Pull requests

Pull requests must:

- Explain the problem and the proposed solution.
- Keep the project buildable.
- Update tests when behavior changes.
- Update documentation when the user experience, privacy behavior, release process, or installation process changes.
- Include screenshots for visual changes.
- Confirm that no credentials were added.

Changes to `.github/workflows/`, release scripts, the installer, the updater, code signing, or security policies require special maintainer review.
