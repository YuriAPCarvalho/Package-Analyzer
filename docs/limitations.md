# Current Limitations

- A project is registered immediately after its folder is selected; manual adjustments are available on the `Settings` tab.
- The dashboard uses the project's latest completed scan.
- The `History` tab lists scans, but opening an older scan as a standalone view has not yet been implemented.
- Misconfigurations and secrets appear on dedicated tabs, but their visual components are still basic.
- Secrets are masked before a finding is persisted, and the saved JSON is redacted in the `Secrets.Match` and `Code.Lines.Content` fields.
- Automatic retention removes older scans and their associated files according to the configured policy.
- Automatic preparation does not install the .NET SDK, Node.js, or a JDK. Maven and Gradle wrappers may download their declared build-tool distributions.
- Project tests are not executed by automatic preparation; it performs dependency restore/install and build only.
- The `Microsoft.EntityFrameworkCore.Design` package was removed from the runtime to avoid a problematic transitive target in this environment; the initial migration remains manually versioned.
