# Third-party notices

Package-Analyzer depends on open-source components that remain the property of their respective authors.

License information should be reviewed again before the first public signed release.

| Component | Purpose | Official URL | License | Notes |
| --- | --- | --- | --- | --- |
| Avalonia | Desktop UI framework | https://avaloniaui.net/ | MIT | Referenced by the app project. |
| CommunityToolkit.Mvvm | MVVM helpers | https://github.com/CommunityToolkit/dotnet | MIT | Referenced by the app project. |
| Entity Framework Core SQLite | SQLite persistence | https://learn.microsoft.com/ef/core/ | MIT | Referenced by the infrastructure and integration test projects. |
| Microsoft.Extensions.DependencyInjection | Dependency injection | https://github.com/dotnet/runtime | MIT | Referenced by the app project. |
| Microsoft.Extensions.Logging | Logging abstractions and integration | https://github.com/dotnet/runtime | MIT | Referenced by the app and infrastructure projects. |
| SQLite | Local database engine | https://www.sqlite.org/ | Public domain / blessing | Used through EF Core SQLite. Review bundled native assets before signing. |
| Velopack | Windows installer and update packaging | https://velopack.io/ | MIT | Referenced by the app and release workflow. |
| Trivy | Local vulnerability, misconfiguration, and secret scanner | https://github.com/aquasecurity/trivy | Apache-2.0 | Downloaded or configured separately as an upstream Aqua Security component. |
| xUnit | Test framework | https://xunit.net/ | Apache-2.0 | Test-only dependency. |
| coverlet.collector | Test coverage collector | https://github.com/coverlet-coverage/coverlet | MIT | Test-only dependency. |

Package-Analyzer is an independent open-source project and is not affiliated with or endorsed by Aqua Security, Microsoft, GitHub, Avalonia, Velopack, SQLite, xUnit, or SignPath.
