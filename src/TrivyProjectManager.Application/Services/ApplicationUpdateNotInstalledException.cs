namespace TrivyProjectManager.Application.Services;

public sealed class ApplicationUpdateNotInstalledException : Exception
{
    public ApplicationUpdateNotInstalledException(string message)
        : base(message)
    {
    }
}
