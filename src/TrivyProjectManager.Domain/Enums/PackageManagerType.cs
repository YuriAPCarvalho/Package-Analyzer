namespace TrivyProjectManager.Domain.Enums;

public enum PackageManagerType
{
    Unknown = 0,
    DotNetCli = 1,
    Npm = 2,
    Pnpm = 3,
    Yarn = 4,
    Maven = 5,
    Gradle = 6,
    Multiple = 7
}
