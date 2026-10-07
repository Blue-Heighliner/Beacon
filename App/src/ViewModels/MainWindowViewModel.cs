namespace BlueHeighliner.Beacon.ViewModels;

/// <summary>Prepares the app on startup and exposes whichever screen the navigation is showing.</summary>
internal sealed class MainWindowViewModel : ViewModelBase
{
    private readonly IDatabase database;

    private readonly IAuthService auth;

    private readonly INavigation navigation;

    /// <summary>Initializes a new instance of the <see cref="MainWindowViewModel" /> class.</summary>
    /// <param name="database">The database to prepare on startup.</param>
    /// <param name="auth">Ensures the built-in admin account exists.</param>
    /// <param name="navigation">Supplies and changes the current screen.</param>
    public MainWindowViewModel(IDatabase database, IAuthService auth, INavigation navigation)
    {
        this.database = database;
        this.auth = auth;
        this.navigation = navigation;
        navigation.Changed += () => OnPropertyChanged(nameof(CurrentViewModel));
    }

    /// <summary>Gets the view model of the screen being shown.</summary>
    public ViewModelBase? CurrentViewModel => navigation.Current;

    /// <summary>Prepares the database and admin account, then shows the login screen.</summary>
    /// <param name="cancellation">Cancels startup.</param>
    public async Task Start(CancellationToken cancellation = default)
    {
        await database.Initialize(cancellation);
        await auth.EnsureAdminAccount(cancellation);
        await navigation.ShowLogin();
    }
}
