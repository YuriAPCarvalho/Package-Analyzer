# SignPath Foundation Application Checklist

- [x] Main repository is public
- [x] GitHub detects the MIT license
- [x] Complete source code is published
- [x] Build scripts are published
- [x] GitHub Actions workflows are published
- [x] README is complete
- [x] Download page is documented through GitHub Releases in the main repository
- [x] First public release is available
- [x] Privacy policy is published
- [x] Code signing policy is published
- [x] Committer, reviewer, and approver roles are published
- [ ] 2FA is enabled on GitHub
- [ ] 2FA is configured for the SignPath account
- [x] Build is automated and verifiable
- [x] Executable metadata is consistent
- [x] Changelog is available
- [x] SHA-256 checksums are published
- [x] `THIRD_PARTY_NOTICES.md` has been reviewed
- [x] No secrets exist in the code or repository history
- [x] Release points to the exact source code
- [x] A release has been published in the format that will be signed
- [ ] SignPath Foundation application has been submitted manually

## Status verified on 2026-08-05

- The `YuriAPCarvalho/Package-Analyzer` repository is public, and GitHub detects the MIT license.
- The `YuriAPCarvalho/Package-Analyzer-Download` repository was not found and is no longer part of the release process.
- The `v0.1.0` tag remains on commit `14d075a`, but no GitHub release was published for it.
- The CI and release workflows are published and have completed successfully.
- Release `v0.1.1` was published from commit `eee351b2ea5a57e4325800a97029d2544e69c235`.
- `SHA256SUMS.txt` was published, and its six hashes match the digests of the assets stored by GitHub.
- Private Vulnerability Reporting remains disabled and must be enabled manually.
- Artifacts remain unsigned; SignPath Foundation integration remains planned / application pending.

## Information to enter manually

- Repository URL: `https://github.com/YuriAPCarvalho/Package-Analyzer`
- Download page URL: `https://github.com/YuriAPCarvalho/Package-Analyzer/releases`
- Project description: local-first desktop application for scanning project security with a local Trivy installation, scan history, scan comparison, and remediation recommendations.
- License: MIT
- Maintainer: YuriAPCarvalho / Yuri Alexandre Pires de Carvalho
- Code signing policy: `CODE_SIGNING_POLICY.md`
- Privacy policy: `PRIVACY.md`
- Build workflow: `.github/workflows/release.yml`
- Example release: `https://github.com/YuriAPCarvalho/Package-Analyzer/releases/tag/v0.1.1`

## Points requiring attention

- Enabling 2FA is a manual task.
- Enabling Private Vulnerability Reporting is a manual task.
- Applying to the SignPath Foundation is a manual task.
- Every signing request must require manual maintainer approval.
- `trivy.exe` is an upstream Aqua Security component and must not be treated as a Package-Analyzer binary.
