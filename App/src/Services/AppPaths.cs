namespace BlueHeighliner.Beacon.Services;

/// <summary>Locates the files the app keeps on disk.</summary>
internal interface IAppPaths
{
    /// <summary>Gets the folder holding all of the app's data.</summary>
    string DataDirectory { get; }

    /// <summary>Gets the path of the SQLite database file.</summary>
    string DatabasePath { get; }

    /// <summary>Gets the path of the file holding the encryption key on platforms without DPAPI.</summary>
    string KeyPath { get; }
}

internal sealed class AppPaths(string dataDirectory) : IAppPaths
{
    public string DataDirectory => dataDirectory;

    public string DatabasePath { get; } = Path.Combine(dataDirectory, "inventory.db");

    public string KeyPath { get; } = Path.Combine(dataDirectory, "dodid.key");
}
