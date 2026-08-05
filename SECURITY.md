# Security Policy

## Supported versions

Security fixes are provided for the latest public release and the current `main` branch.

## Reporting a vulnerability

Do not open a public issue for an exploitable vulnerability or for a report that includes secrets, private paths, private source code, or unredacted scan output.

Use a private GitHub Security Advisory in this repository.

If private vulnerability reporting is not available yet, the maintainer should enable it manually in GitHub:

1. Open the repository on GitHub.
2. Go to `Settings`.
3. Open `Code security and analysis`.
4. Enable `Private vulnerability reporting`.

## Handling expectations

Reports should include:

- affected version;
- Windows version;
- sanitized reproduction steps;
- expected and observed behavior;
- sanitized logs or screenshots when useful.

Security-sensitive workflow, release, installer, updater, and signing changes require special maintainer review.
