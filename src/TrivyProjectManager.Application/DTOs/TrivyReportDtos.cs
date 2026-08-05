using System.Text.Json.Serialization;

namespace TrivyProjectManager.Application.DTOs;

public sealed class TrivyReportDto
{
    [JsonPropertyName("SchemaVersion")]
    public int? SchemaVersion { get; set; }

    [JsonPropertyName("ArtifactName")]
    public string? ArtifactName { get; set; }

    [JsonPropertyName("ArtifactType")]
    public string? ArtifactType { get; set; }

    [JsonPropertyName("Metadata")]
    public TrivyMetadataDto? Metadata { get; set; }

    [JsonPropertyName("Results")]
    public List<TrivyResultDto>? Results { get; set; }
}

public sealed class TrivyMetadataDto
{
    [JsonPropertyName("ImageID")]
    public string? ImageId { get; set; }
}

public sealed class TrivyResultDto
{
    [JsonPropertyName("Target")]
    public string? Target { get; set; }

    [JsonPropertyName("Class")]
    public string? Class { get; set; }

    [JsonPropertyName("Type")]
    public string? Type { get; set; }

    [JsonPropertyName("Vulnerabilities")]
    public List<TrivyVulnerabilityDto>? Vulnerabilities { get; set; }

    [JsonPropertyName("Misconfigurations")]
    public List<TrivyMisconfigurationDto>? Misconfigurations { get; set; }

    [JsonPropertyName("Secrets")]
    public List<TrivySecretDto>? Secrets { get; set; }
}

public sealed class TrivyVulnerabilityDto
{
    [JsonPropertyName("VulnerabilityID")]
    public string? VulnerabilityId { get; set; }

    [JsonPropertyName("PkgName")]
    public string? PackageName { get; set; }

    [JsonPropertyName("PkgPath")]
    public string? PackagePath { get; set; }

    [JsonPropertyName("InstalledVersion")]
    public string? InstalledVersion { get; set; }

    [JsonPropertyName("FixedVersion")]
    public string? FixedVersion { get; set; }

    [JsonPropertyName("Severity")]
    public string? Severity { get; set; }

    [JsonPropertyName("SeveritySource")]
    public string? SeveritySource { get; set; }

    [JsonPropertyName("Status")]
    public string? Status { get; set; }

    [JsonPropertyName("Title")]
    public string? Title { get; set; }

    [JsonPropertyName("Description")]
    public string? Description { get; set; }

    [JsonPropertyName("PrimaryURL")]
    public string? PrimaryUrl { get; set; }

    [JsonPropertyName("References")]
    public List<string>? References { get; set; }

    [JsonPropertyName("CVSS")]
    public Dictionary<string, TrivyCvssDto>? Cvss { get; set; }

    [JsonPropertyName("CweIDs")]
    public List<string>? CweIds { get; set; }

    [JsonPropertyName("PublishedDate")]
    public DateTimeOffset? PublishedDate { get; set; }

    [JsonPropertyName("LastModifiedDate")]
    public DateTimeOffset? LastModifiedDate { get; set; }
}

public sealed class TrivyCvssDto
{
    [JsonPropertyName("V2Vector")]
    public string? V2Vector { get; set; }

    [JsonPropertyName("V3Vector")]
    public string? V3Vector { get; set; }

    [JsonPropertyName("V40Vector")]
    public string? V40Vector { get; set; }

    [JsonPropertyName("V2Score")]
    public decimal? V2Score { get; set; }

    [JsonPropertyName("V3Score")]
    public decimal? V3Score { get; set; }

    [JsonPropertyName("V40Score")]
    public decimal? V40Score { get; set; }
}

public sealed class TrivyMisconfigurationDto
{
    [JsonPropertyName("ID")]
    public string? Id { get; set; }

    [JsonPropertyName("AVDID")]
    public string? AvdId { get; set; }

    [JsonPropertyName("Type")]
    public string? Type { get; set; }

    [JsonPropertyName("Title")]
    public string? Title { get; set; }

    [JsonPropertyName("Description")]
    public string? Description { get; set; }

    [JsonPropertyName("Message")]
    public string? Message { get; set; }

    [JsonPropertyName("Severity")]
    public string? Severity { get; set; }

    [JsonPropertyName("PrimaryURL")]
    public string? PrimaryUrl { get; set; }

    [JsonPropertyName("References")]
    public List<string>? References { get; set; }

    [JsonPropertyName("CauseMetadata")]
    public TrivyCauseMetadataDto? CauseMetadata { get; set; }
}

public sealed class TrivyCauseMetadataDto
{
    [JsonPropertyName("Resource")]
    public string? Resource { get; set; }

    [JsonPropertyName("Provider")]
    public string? Provider { get; set; }

    [JsonPropertyName("Service")]
    public string? Service { get; set; }

    [JsonPropertyName("StartLine")]
    public int? StartLine { get; set; }

    [JsonPropertyName("Code")]
    public TrivyCodeDto? Code { get; set; }
}

public sealed class TrivyCodeDto
{
    [JsonPropertyName("Lines")]
    public List<TrivyCodeLineDto>? Lines { get; set; }
}

public sealed class TrivyCodeLineDto
{
    [JsonPropertyName("Number")]
    public int? Number { get; set; }

    [JsonPropertyName("Content")]
    public string? Content { get; set; }

    [JsonPropertyName("IsCause")]
    public bool? IsCause { get; set; }
}

public sealed class TrivySecretDto
{
    [JsonPropertyName("RuleID")]
    public string? RuleId { get; set; }

    [JsonPropertyName("Category")]
    public string? Category { get; set; }

    [JsonPropertyName("Severity")]
    public string? Severity { get; set; }

    [JsonPropertyName("Title")]
    public string? Title { get; set; }

    [JsonPropertyName("StartLine")]
    public int? StartLine { get; set; }

    [JsonPropertyName("EndLine")]
    public int? EndLine { get; set; }

    [JsonPropertyName("Match")]
    public string? Match { get; set; }

    [JsonPropertyName("Code")]
    public TrivyCodeDto? Code { get; set; }
}
