namespace BlueHeighliner.Beacon.Tests;

/// <summary>Configures the real application on Avalonia's headless platform for UI tests.</summary>
public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
