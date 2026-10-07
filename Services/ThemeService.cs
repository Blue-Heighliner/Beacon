using Avalonia;
using Avalonia.Styling;

namespace Artemis.Services;

/// <summary>Switches between the Navy/Steel-Blue light theme and the Graphite/Amber dark theme.</summary>
public static class ThemeService
{
    public static bool IsDark =>
        Application.Current?.RequestedThemeVariant == ThemeVariant.Dark;

    public static void Toggle()
    {
        if (Application.Current is { } app)
            app.RequestedThemeVariant = IsDark ? ThemeVariant.Light : ThemeVariant.Dark;
    }
}
