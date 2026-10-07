namespace BlueHeighliner.Beacon.ViewModels;

/// <summary>Administrator screen for editing, resetting, and deleting other users.</summary>
internal sealed partial class UserManagementViewModel(IAuthService auth, IUserRepository users, Func<Task> onBack) : ViewModelBase
{
    [ObservableProperty]
    private User? selectedUser;

    [ObservableProperty]
    private string editUsername = "";

    [ObservableProperty]
    private string editDodId = "";

    [ObservableProperty]
    private string resetPassword = "";

    [ObservableProperty]
    private string resetBadge = "";

    [ObservableProperty]
    private string statusMessage = "";

    [ObservableProperty]
    private bool statusIsError;

    /// <summary>Gets the listed users.</summary>
    public ObservableCollection<User> Users { get; } = [];

    /// <summary>Gets a value indicating whether a user is selected.</summary>
    public bool HasSelection => SelectedUser is not null;

    /// <summary>Gets a value indicating whether the selected user can be deleted; admin accounts never can.</summary>
    public bool CanDeleteSelected => SelectedUser is { IsAdmin: false };

    /// <summary>Loads the user list.</summary>
    public Task Load() => RefreshUsers();

    partial void OnSelectedUserChanged(User? value)
    {
        EditUsername = value?.Username ?? "";
        EditDodId = value?.DodId ?? "";
        ResetPassword = "";
        ResetBadge = "";
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(CanDeleteSelected));
    }

    [RelayCommand]
    private async Task SaveUserInfo()
    {
        if (SelectedUser is not { } user)
        {
            return;
        }

        (bool success, string error) = await auth.AdminUpdateUser(user, EditUsername, EditDodId);
        if (!success)
        {
            ShowStatus(error, isError: true);
            return;
        }

        ShowStatus($"Updated '{user.Username}'.", isError: false);
        await RefreshUsers(user.Id);
    }

    [RelayCommand]
    private async Task ApplyPasswordReset()
    {
        if (SelectedUser is not { } user)
        {
            return;
        }

        (bool success, string error) = await auth.AdminResetPassword(user, ResetPassword);
        ResetPassword = "";
        if (!success)
        {
            ShowStatus(error, isError: true);
            return;
        }

        ShowStatus($"Password reset for '{user.Username}'.", isError: false);
        await RefreshUsers(user.Id);
    }

    [RelayCommand]
    private async Task ApplyBadgeReset()
    {
        if (SelectedUser is not { } user)
        {
            return;
        }

        (bool success, string error) = await auth.AdminResetBadge(user, ResetBadge);
        ResetBadge = "";
        if (!success)
        {
            ShowStatus(error, isError: true);
            return;
        }

        ShowStatus($"Badge updated for '{user.Username}'.", isError: false);
        await RefreshUsers(user.Id);
    }

    [RelayCommand]
    private async Task DeleteSelectedUser()
    {
        if (SelectedUser is not { } user)
        {
            return;
        }

        (bool success, string error) = await auth.AdminDeleteUser(user);
        if (!success)
        {
            ShowStatus(error, isError: true);
            return;
        }

        ShowStatus($"Deleted user '{user.Username}'.", isError: false);
        await RefreshUsers();
    }

    [RelayCommand]
    private Task Back() => onBack();

    private async Task RefreshUsers(long? keepSelectedId = null)
    {
        List<User> loaded = await users.GetAll();
        Users.Clear();
        User? reselect = null;
        foreach (User user in loaded)
        {
            Users.Add(user);
            if (user.Id == keepSelectedId)
            {
                reselect = user;
            }
        }

        SelectedUser = reselect;
    }

    private void ShowStatus(string message, bool isError)
    {
        StatusMessage = message;
        StatusIsError = isError;
    }
}
