using Artemis.Data;
using Artemis.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Artemis.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly AuthService _auth = new();
    private readonly ItemRepository _items = new();
    private readonly UserRepository _users = new();

    [ObservableProperty]
    private ViewModelBase? _currentViewModel;

    public MainWindowViewModel()
    {
        _auth.EnsureAdminAccount();
        ShowLogin();
    }

    private void ShowLogin() => CurrentViewModel = new LoginViewModel(_auth, ShowInventory);

    private void ShowInventory() =>
        CurrentViewModel = new InventoryViewModel(_auth, _items, ShowLogin, ShowAccount, ShowUserManagement);

    private void ShowAccount() => CurrentViewModel = new AccountViewModel(_auth, ShowInventory);

    private void ShowUserManagement() =>
        CurrentViewModel = new UserManagementViewModel(_auth, _users, ShowInventory);
}
