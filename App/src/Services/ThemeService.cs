namespace BlueHeighliner.Beacon.Services;

/// <summary>Switches between the Navy/Steel-Blue light theme and the Graphite/Amber dark theme.</summary>
internal interface IThemeService
{
    /// <summary>Gets a value indicating whether the dark theme is active.</summary>
    bool IsDark { get; }

    /// <summary>Switches to the other theme.</summary>
    void Toggle();
}

internal sealed class ThemeService : IThemeService
{
    public bool IsDark => Application.Current?.RequestedThemeVariant == ThemeVariant.Dark;

    public void Toggle()
    {
        if (Application.Current is { } app)
        {
            app.RequestedThemeVariant = IsDark ? ThemeVariant.Light : ThemeVariant.Dark;
        }
    }
}
