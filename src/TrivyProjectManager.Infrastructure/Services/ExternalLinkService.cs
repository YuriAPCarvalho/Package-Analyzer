using System.Diagnostics;
using TrivyProjectManager.Application.Abstractions;

namespace TrivyProjectManager.Infrastructure.Services;

public sealed class ExternalLinkService : IExternalLinkService
{
    public Task OpenAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("Only HTTP and HTTPS URLs can be opened.");
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = uri.ToString(),
            UseShellExecute = true
        });

        return Task.CompletedTask;
    }
}
