namespace BlueHeighliner.Beacon.ViewModels;

/// <summary>Owns navigation between the login, inventory, account, and user management screens.</summary>
internal sealed partial class MainWindowViewModel(IDatabase database, IAuthService auth, IItemRepository items, IUserRepository users, IThemeService theme, IInputValidator validator, IExcelExportService excel) : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase? currentViewModel;

    /// <summary>Prepares the database and admin account, then shows the login screen.</summary>
    /// <param name="cancellation">Cancels startup.</param>
    public async Task Start(CancellationToken cancellation = default)
    {
        await database.Initialize(cancellation);
        await auth.EnsureAdminAccount(cancellation);
        await ShowLogin();
    }

    private Task ShowLogin()
    {
        CurrentViewModel = new LoginViewModel(auth, theme, ShowInventory);
        return Task.CompletedTask;
    }

    private async Task ShowInventory()
    {
        InventoryViewModel inventory = new(auth, items, theme, validator, excel, ShowLogin, ShowAccount, ShowUserManagement);
        await inventory.Load();
        CurrentViewModel = inventory;
    }

    private Task ShowAccount()
    {
        CurrentViewModel = new AccountViewModel(auth, ShowInventory);
        return Task.CompletedTask;
    }

    private async Task ShowUserManagement()
    {
        UserManagementViewModel management = new(auth, users, ShowInventory);
        await management.Load();
        CurrentViewModel = management;
    }
}
