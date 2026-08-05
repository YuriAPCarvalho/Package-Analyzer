using System.Xml.Linq;

namespace TrivyProjectManager.UnitTests;

public sealed class RepositoryMetadataTests
{
    [Fact]
    public void BuildMetadataIdentifiesMainRepositoryAndMitLicense()
    {
        var props = XDocument.Load(RepositoryFile("Directory.Build.props"));
        var properties = props.Root!
            .Elements("PropertyGroup")
            .Elements()
            .ToDictionary(element => element.Name.LocalName, element => element.Value);

        Assert.Equal("Package-Analyzer", properties["Product"]);
        Assert.Equal("YuriAPCarvalho", properties["Company"]);
        Assert.Equal("Yuri Alexandre Pires de Carvalho", properties["Authors"]);
        Assert.Equal("MIT", properties["PackageLicenseExpression"]);
        Assert.Equal("https://github.com/YuriAPCarvalho/Package-Analyzer", properties["RepositoryUrl"]);
        Assert.Equal("git", properties["RepositoryType"]);
    }

    [Fact]
    public void ReleaseWorkflowDerivesVersionMetadataFromSemVerTag()
    {
        var workflow = File.ReadAllText(RepositoryFile(".github", "workflows", "release.yml"));

        Assert.Contains("^v(?<version>\\d+\\.\\d+\\.\\d+)$", workflow);
        Assert.DoesNotContain("PUBLIC_RELEASE_TOKEN", workflow);
        Assert.DoesNotContain("Package-Analyzer-Download", workflow);
        Assert.Contains("-p:Version=${{ steps.version.outputs.version }}", workflow);
        Assert.Contains("-p:AssemblyVersion=${{ steps.version.outputs.assembly_version }}", workflow);
        Assert.Contains("-p:FileVersion=${{ steps.version.outputs.assembly_version }}", workflow);
        Assert.Contains("-p:InformationalVersion=${{ steps.version.outputs.version }}", workflow);
        Assert.Contains("SHA256SUMS.txt", workflow);
    }

    private static string RepositoryFile(params string[] segments)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return Path.Combine(new[] { root }.Concat(segments).ToArray());
    }
}
