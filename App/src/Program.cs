namespace BlueHeighliner.Beacon;

/// <summary>Application entry point.</summary>
internal sealed class Program
{
    /// <summary>Starts the application with the classic desktop lifetime.</summary>
    /// <param name="args">The command line arguments.</param>
    /// <returns>The exit code.</returns>
    [STAThread]
    public static int Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    /// <summary>Builds the Avalonia configuration; also used by the visual designer.</summary>
    /// <returns>The configured app builder.</returns>
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
}
