namespace BlueHeighliner.Beacon.Services;

/// <summary>A screen that loads its data before being shown.</summary>
internal interface ILoadable
{
    /// <summary>Loads the screen's data.</summary>
    Task Load();
}

/// <summary>Owns which screen is showing and moves between screens.</summary>
internal interface INavigation
{
    /// <summary>Raised after <see cref="Current" /> changes.</summary>
    event Action? Changed;

    /// <summary>Gets the view model of the screen being shown, or null before the first navigation.</summary>
    ViewModelBase? Current { get; }

    /// <summary>Shows the login screen.</summary>
    Task ShowLogin();

    /// <summary>Shows the inventory screen, loaded.</summary>
    Task ShowInventory();

    /// <summary>Shows the account screen.</summary>
    Task ShowAccount();

    /// <summary>Shows the user management screen, loaded.</summary>
    Task ShowUserManagement();
}

internal sealed class Navigation(IServiceProvider services) : INavigation
{
    public event Action? Changed;

    public ViewModelBase? Current { get; private set; }

    public Task ShowLogin() => Show<LoginViewModel>();

    public Task ShowInventory() => Show<InventoryViewModel>();

    public Task ShowAccount() => Show<AccountViewModel>();

    public Task ShowUserManagement() => Show<UserManagementViewModel>();

    private async Task Show<TViewModel>()
        where TViewModel : ViewModelBase
    {
        TViewModel viewModel = services.GetRequiredService<TViewModel>();
        if (viewModel is ILoadable loadable)
        {
            await loadable.Load();
        }

        Current = viewModel;
        Changed?.Invoke();
    }
}
