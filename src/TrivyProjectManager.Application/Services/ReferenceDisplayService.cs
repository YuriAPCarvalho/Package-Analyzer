using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Domain.Enums;

namespace TrivyProjectManager.Application.Services;

public sealed class ReferenceDisplayService
{
    public IReadOnlyList<FindingReference> Build(IEnumerable<string> urls, string? primaryUrl = null)
    {
        var references = urls
            .Where(IsHttpsUrl)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(url =>
            {
                var kind = Classify(url);
                return new FindingReference
                {
                    Url = url,
                    DisplayName = DisplayName(kind, url),
                    SourceKind = kind
                };
            })
            .ToList();

        if (!string.IsNullOrWhiteSpace(primaryUrl) && IsHttpsUrl(primaryUrl) && references.All(reference => !reference.Url.Equals(primaryUrl, StringComparison.OrdinalIgnoreCase)))
        {
            var kind = Classify(primaryUrl);
            references.Add(new FindingReference
            {
                Url = primaryUrl,
                DisplayName = DisplayName(kind, primaryUrl),
                SourceKind = kind
            });
        }

        var primary = references
            .OrderBy(Priority)
            .ThenBy(reference => reference.DisplayName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        if (primary is not null)
        {
            primary.IsPrimary = true;
        }

        return references
            .OrderByDescending(reference => reference.IsPrimary)
            .ThenBy(Priority)
            .ThenBy(reference => reference.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public string? SelectPrimaryUrl(IEnumerable<FindingReference> references)
    {
        return references.OrderByDescending(reference => reference.IsPrimary).ThenBy(Priority).FirstOrDefault()?.Url;
    }

    public static bool IsHttpsUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
    }

    private static FindingReferenceSourceKind Classify(string url)
    {
        var host = new Uri(url).Host.ToLowerInvariant();
        if (host.Contains("msrc.microsoft.com", StringComparison.OrdinalIgnoreCase)
            || host.Contains("learn.microsoft.com", StringComparison.OrdinalIgnoreCase)
            || host.Contains("portal.msrc.microsoft.com", StringComparison.OrdinalIgnoreCase))
        {
            return FindingReferenceSourceKind.Microsoft;
        }

        if (host.Contains("github.com", StringComparison.OrdinalIgnoreCase) && url.Contains("/advisories/", StringComparison.OrdinalIgnoreCase))
        {
            return FindingReferenceSourceKind.GitHubAdvisory;
        }

        if (host.Contains("nvd.nist.gov", StringComparison.OrdinalIgnoreCase))
        {
            return FindingReferenceSourceKind.Nvd;
        }

        if (host.Contains("cve.org", StringComparison.OrdinalIgnoreCase))
        {
            return FindingReferenceSourceKind.CveOrg;
        }

        if (host.Contains("access.redhat.com", StringComparison.OrdinalIgnoreCase) || host.Contains("redhat.com", StringComparison.OrdinalIgnoreCase))
        {
            return FindingReferenceSourceKind.RedHat;
        }

        if (host.Contains("osv.dev", StringComparison.OrdinalIgnoreCase))
        {
            return FindingReferenceSourceKind.Osv;
        }

        return host.Contains("microsoft.com", StringComparison.OrdinalIgnoreCase)
            ? FindingReferenceSourceKind.Microsoft
            : FindingReferenceSourceKind.Other;
    }

    private static string DisplayName(FindingReferenceSourceKind kind, string url)
    {
        return kind switch
        {
            FindingReferenceSourceKind.Microsoft => "Microsoft Security Advisory",
            FindingReferenceSourceKind.GitHubAdvisory => "GitHub Advisory",
            FindingReferenceSourceKind.Nvd => "NVD",
            FindingReferenceSourceKind.CveOrg => "CVE.org",
            FindingReferenceSourceKind.RedHat => "Red Hat Security",
            FindingReferenceSourceKind.Osv => "OSV",
            _ => new Uri(url).Host
        };
    }

    private static int Priority(FindingReference reference)
    {
        return reference.SourceKind switch
        {
            FindingReferenceSourceKind.Microsoft => 0,
            FindingReferenceSourceKind.Vendor => 0,
            FindingReferenceSourceKind.GitHubAdvisory => 1,
            FindingReferenceSourceKind.Nvd => 2,
            FindingReferenceSourceKind.CveOrg => 3,
            _ => 4
        };
    }
}
