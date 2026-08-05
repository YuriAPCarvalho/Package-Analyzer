# Pull request

## Summary

Describe the change and why it is needed.

## Validation

- [ ] `dotnet restore TrivyProjectManager.sln -m:1 -nr:false`
- [ ] `dotnet build TrivyProjectManager.sln --configuration Release --no-restore -m:1 -nr:false`
- [ ] `dotnet test TrivyProjectManager.sln --configuration Release --no-build -m:1 -nr:false`

## Checklist

- [ ] No credentials, tokens, private keys, private paths, or unredacted secrets were added.
- [ ] Documentation was updated when behavior, release, privacy, installation, or UI changed.
- [ ] UI changes include screenshots.
- [ ] New behavior includes tests.
- [ ] Workflow, release, updater, installer, and signing changes received special maintainer review.
- [ ] The privacy policy remains accurate.
