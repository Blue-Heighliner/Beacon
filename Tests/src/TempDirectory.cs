namespace BlueHeighliner.Beacon.Tests;

/// <summary>A uniquely named scratch folder that is deleted on dispose.</summary>
internal sealed class TempDirectory : IDisposable
{
    public TempDirectory() => Directory.CreateDirectory(Path);

    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beacon-tests-" + Guid.NewGuid().ToString("N"));

    public string File(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
