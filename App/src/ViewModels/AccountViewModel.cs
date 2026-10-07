namespace BlueHeighliner.Beacon.ViewModels;

/// <summary>Self-service password and badge changes for the signed-in user.</summary>
internal sealed partial class AccountViewModel(IAuthService auth, Func<Task> onBack) : ViewModelBase
{
    [ObservableProperty]
    private string currentPassword = "";

    [ObservableProperty]
    private string newPassword = "";

    [ObservableProperty]
    private string newPasswordConfirm = "";

    [ObservableProperty]
    private string badgeInput = "";

    [ObservableProperty]
    private string statusMessage = "";

    [ObservableProperty]
    private bool statusIsError;

    /// <summary>Gets the signed-in user's username.</summary>
    public string Username => auth.CurrentUser?.Username ?? "";

    /// <summary>Gets the signed-in user's DOD ID.</summary>
    public string DodId => auth.CurrentUser?.DodId ?? "";

    /// <summary>Gets a value indicating whether the signed-in user has a badge registered.</summary>
    public bool HasBadge => auth.CurrentUser?.BarcodeHash is not null;

    [RelayCommand]
    private async Task ChangePassword()
    {
        if (NewPassword != NewPasswordConfirm)
        {
            ShowStatus("New passwords do not match.", isError: true);
            return;
        }

        (bool success, string error) = await auth.ChangePassword(CurrentPassword, NewPassword);
        if (!success)
        {
            ShowStatus(error, isError: true);
            return;
        }

        CurrentPassword = NewPassword = NewPasswordConfirm = "";
        ShowStatus("Password changed.", isError: false);
    }

    [RelayCommand]
    private async Task ChangeBadge()
    {
        (bool success, string error) = await auth.ChangeBadge(BadgeInput);
        BadgeInput = "";
        if (!success)
        {
            ShowStatus(error, isError: true);
            return;
        }

        OnPropertyChanged(nameof(HasBadge));
        ShowStatus("Badge updated.", isError: false);
    }

    [RelayCommand]
    private Task Back() => onBack();

    private void ShowStatus(string message, bool isError)
    {
        StatusMessage = message;
        StatusIsError = isError;
    }
}
