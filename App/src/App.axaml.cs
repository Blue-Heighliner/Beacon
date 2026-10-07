namespace BlueHeighliner.Beacon;

/// <summary>The Avalonia application; composes the services and view models and shows the main window.</summary>
internal sealed partial class App : Application
{
    /// <summary>Gets the service provider; null until the desktop lifetime has initialized.</summary>
    public ServiceProvider? Services { get; private set; }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            Services = CreateServices();
            desktop.MainWindow = new MainWindow { DataContext = Services.GetRequiredService<MainWindowViewModel>() };
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>Builds the service provider, storing data under the local application data folder.</summary>
    /// <returns>The provider.</returns>
    internal static ServiceProvider CreateServices()
    {
        // LocalApplicationData rather than next to the exe because cloud sync on a live SQLite file risks lock errors.
        string dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Beacon");
        return new ServiceCollection().AddBeacon(dataDirectory).BuildServiceProvider();
    }

    /// <summary>Removes Avalonia's data annotation validation so it does not duplicate the CommunityToolkit's.</summary>
    internal void DisableAvaloniaDataAnnotationValidation()
    {
        DataAnnotationsValidationPlugin[] dataValidationPluginsToRemove = [.. BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>()];

        foreach (DataAnnotationsValidationPlugin plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
