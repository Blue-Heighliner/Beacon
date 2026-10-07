namespace BlueHeighliner.Beacon.ViewModels;

/// <summary>Sign-in by password or badge, and new account registration.</summary>
internal sealed partial class LoginViewModel(IAuthService auth, IThemeService theme, Func<Task> onLoginSucceeded) : ViewModelBase
{
    [ObservableProperty]
    private string username = "";

    [ObservableProperty]
    private string password = "";

    [ObservableProperty]
    private string badgeInput = "";

    [ObservableProperty]
    private string regUsername = "";

    [ObservableProperty]
    private string regDodId = "";

    [ObservableProperty]
    private string regPassword = "";

    [ObservableProperty]
    private string regPasswordConfirm = "";

    [ObservableProperty]
    private string regBadge = "";

    [ObservableProperty]
    private string statusMessage = "";

    [ObservableProperty]
    private bool statusIsError;

    [RelayCommand]
    private async Task Login()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrEmpty(Password))
        {
            ShowError("Enter your username and password.");
            return;
        }

        if (await auth.LoginWithPassword(Username, Password))
        {
            await onLoginSucceeded();
        }
        else
        {
            ShowError("Invalid username or password.");
        }
    }

    [RelayCommand]
    private async Task BadgeLogin()
    {
        if (string.IsNullOrWhiteSpace(BadgeInput))
        {
            return;
        }

        if (await auth.LoginWithBarcode(BadgeInput))
        {
            await onLoginSucceeded();
        }
        else
        {
            BadgeInput = "";
            ShowError("Badge not recognized.");
        }
    }

    [RelayCommand]
    private async Task Register()
    {
        if (!string.IsNullOrEmpty(RegPassword) && RegPassword != RegPasswordConfirm)
        {
            ShowError("Passwords do not match.");
            return;
        }

        (bool success, string error) = await auth.Register(RegUsername, RegPassword, RegBadge, RegDodId);
        if (!success)
        {
            ShowError(error);
            return;
        }

        StatusMessage = $"Account '{RegUsername.Trim()}' created. You can sign in now.";
        StatusIsError = false;
        Username = RegUsername.Trim();
        RegUsername = RegDodId = RegPassword = RegPasswordConfirm = RegBadge = "";
    }

    [RelayCommand]
    private void ToggleTheme() => theme.Toggle();

    private void ShowError(string message)
    {
        StatusMessage = message;
        StatusIsError = true;
    }
}
