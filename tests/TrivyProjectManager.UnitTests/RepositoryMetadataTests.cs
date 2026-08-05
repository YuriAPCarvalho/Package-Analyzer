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
        Assert.Contains("if: github.repository == 'YuriAPCarvalho/Package-Analyzer'", workflow);
        Assert.Contains("GITHUB_TOKEN: ${{ github.token }}", workflow);
        Assert.Contains("GH_TOKEN: ${{ github.token }}", workflow);
        Assert.Contains("--repo $env:GITHUB_REPOSITORY", workflow);
        Assert.Contains("-p:Version=${{ steps.version.outputs.version }}", workflow);
        Assert.Contains("-p:AssemblyVersion=${{ steps.version.outputs.assembly_version }}", workflow);
        Assert.Contains("-p:FileVersion=${{ steps.version.outputs.assembly_version }}", workflow);
        Assert.Contains("-p:InformationalVersion=${{ steps.version.outputs.version }}", workflow);
        Assert.Contains("SHA256SUMS.txt", workflow);
        Assert.Contains("Signature status: unsigned release artifacts.", workflow);
        Assert.Contains("SignPath Foundation integration: planned / application pending.", workflow);
    }

    [Fact]
    public void ContinuousIntegrationValidatesWorkflowsAndSolution()
    {
        var workflow = File.ReadAllText(RepositoryFile(".github", "workflows", "ci.yml"));

        Assert.Contains("contents: read", workflow);
        Assert.Contains("dotnet-version: 9.0.x", workflow);
        Assert.Contains("actionlint_${version}_windows_amd64.zip", workflow);
        Assert.Contains("6e7241b51e6817ea6a047693d8e6fed13b31819c9a0dd6c5a726e1592d22f6e9", workflow);
        Assert.Contains("dotnet restore TrivyProjectManager.sln -m:1 -nr:false", workflow);
        Assert.Contains("dotnet build TrivyProjectManager.sln --configuration Release --no-restore -m:1 -nr:false", workflow);
        Assert.Contains("dotnet test TrivyProjectManager.sln --configuration Release --no-build -m:1 -nr:false", workflow);
    }

    private static string RepositoryFile(params string[] segments)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return Path.Combine(new[] { root }.Concat(segments).ToArray());
    }
}
