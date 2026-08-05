namespace TrivyProjectManager.Domain.Enums;

public enum FindingReferenceSourceKind
{
    Other = 0,
    Vendor = 1,
    GitHubAdvisory = 2,
    Nvd = 3,
    CveOrg = 4,
    RedHat = 5,
    Microsoft = 6,
    Osv = 7
}
