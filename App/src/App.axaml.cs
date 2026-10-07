namespace BlueHeighliner.Beacon;

/// <summary>The Avalonia application; composes the services and view models and shows the main window.</summary>
internal sealed partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow { DataContext = CreateMainWindowViewModel() };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static MainWindowViewModel CreateMainWindowViewModel()
    {
        // LocalApplicationData rather than next to the exe because cloud sync on a live SQLite file risks lock errors.
        string dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Beacon");

        DodIdCipher cipher = new(Path.Combine(dataDirectory, "dodid.key"));
        Database database = new(Path.Combine(dataDirectory, "inventory.db"), cipher);
        UserRepository users = new(database, cipher);
        ItemRepository items = new(database);
        InputValidator validator = new();
        AuthService auth = new(users, validator);
        return new MainWindowViewModel(database, auth, items, users, new ThemeService(), validator, new ExcelExportService());
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        DataAnnotationsValidationPlugin[] dataValidationPluginsToRemove = [.. BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>()];

        foreach (DataAnnotationsValidationPlugin plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
