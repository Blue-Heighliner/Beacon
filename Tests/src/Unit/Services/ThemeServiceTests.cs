namespace BlueHeighliner.Beacon.Tests.Unit.Services;

public sealed class ThemeServiceTests
{
    [AvaloniaFact]
    public void Toggle_SwitchesBetweenLightAndDark()
    {
        ThemeService theme = new();
        Application.Current!.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;

        theme.Toggle();
        Assert.True(theme.IsDark);

        theme.Toggle();
        Assert.False(theme.IsDark);
    }
}
