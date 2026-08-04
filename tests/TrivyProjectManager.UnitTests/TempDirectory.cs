namespace TrivyProjectManager.UnitTests;

public sealed class TempDirectory : IDisposable
{
    private TempDirectory(string path)
    {
        Path = path;
        Directory.CreateDirectory(path);
    }

    public string Path { get; }

    public static TempDirectory Create()
    {
        return new TempDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tpm-tests", Guid.NewGuid().ToString("N")));
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
