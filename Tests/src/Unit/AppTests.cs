namespace BlueHeighliner.Beacon.Tests.Unit;

public sealed class AppTests
{
    [AvaloniaFact]
    public void Application_IsInitializedWithStyles()
    {
        App app = Assert.IsType<App>(Application.Current);

        Assert.NotEmpty(app.Styles);
    }

    [AvaloniaFact]
    public void Application_DefaultsToTheDarkTheme() => Assert.True(new ThemeService().IsDark);

    [AvaloniaFact]
    public void DisableAvaloniaDataAnnotationValidation_RemovesThePlugin()
    {
        App app = Assert.IsType<App>(Application.Current);

        app.DisableAvaloniaDataAnnotationValidation();

        Assert.DoesNotContain(Avalonia.Data.Core.Plugins.BindingPlugins.DataValidators, p => p is Avalonia.Data.Core.Plugins.DataAnnotationsValidationPlugin);
    }
}
