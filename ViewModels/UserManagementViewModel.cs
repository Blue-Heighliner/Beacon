using System;
using System.Collections.ObjectModel;
using Artemis.Data;
using Artemis.Models;
using Artemis.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Artemis.ViewModels;

public partial class UserManagementViewModel : ViewModelBase
{
    private readonly AuthService _auth;
    private readonly UserRepository _users;
    private readonly Action _onBack;

    public ObservableCollection<User> Users { get; } = new();

    [ObservableProperty] private User? _selectedUser;

    // Edit fields for the selected user
    [ObservableProperty] private string _editUsername = "";
    [ObservableProperty] private string _editDodId = "";
    [ObservableProperty] private string _resetPassword = "";
    [ObservableProperty] private string _resetBadge = "";

    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool _statusIsError;

    public bool HasSelection => SelectedUser is not null;

    /// <summary>Admin accounts can never be deleted from the UI.</summary>
    public bool CanDeleteSelected => SelectedUser is { IsAdmin: false };

    public UserManagementViewModel(AuthService auth, UserRepository users, Action onBack)
    {
        _auth = auth;
        _users = users;
        _onBack = onBack;
        RefreshUsers();
    }

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
    private void SaveUserInfo()
    {
        if (SelectedUser is not { } user)
            return;

        var (success, error) = _auth.AdminUpdateUser(user, EditUsername, EditDodId);
        if (!success)
        {
            ShowStatus(error, isError: true);
            return;
        }

        ShowStatus($"Updated '{user.Username}'.", isError: false);
        RefreshUsers(user.Id);
    }

    [RelayCommand]
    private void ApplyPasswordReset()
    {
        if (SelectedUser is not { } user)
            return;

        var (success, error) = _auth.AdminResetPassword(user, ResetPassword);
        ResetPassword = "";
        if (!success)
        {
            ShowStatus(error, isError: true);
            return;
        }

        ShowStatus($"Password reset for '{user.Username}'.", isError: false);
        RefreshUsers(user.Id);
    }

    [RelayCommand]
    private void ApplyBadgeReset()
    {
        if (SelectedUser is not { } user)
            return;

        var (success, error) = _auth.AdminResetBadge(user, ResetBadge);
        ResetBadge = "";
        if (!success)
        {
            ShowStatus(error, isError: true);
            return;
        }

        ShowStatus($"Badge updated for '{user.Username}'.", isError: false);
        RefreshUsers(user.Id);
    }

    [RelayCommand]
    private void DeleteSelectedUser()
    {
        if (SelectedUser is not { } user)
            return;

        var (success, error) = _auth.AdminDeleteUser(user);
        if (!success)
        {
            ShowStatus(error, isError: true);
            return;
        }

        ShowStatus($"Deleted user '{user.Username}'.", isError: false);
        RefreshUsers();
    }

    [RelayCommand]
    private void Back() => _onBack();

    private void RefreshUsers(long? keepSelectedId = null)
    {
        Users.Clear();
        User? reselect = null;
        foreach (var user in _users.GetAll())
        {
            Users.Add(user);
            if (user.Id == keepSelectedId)
                reselect = user;
        }
        SelectedUser = reselect;
    }

    private void ShowStatus(string message, bool isError)
    {
        StatusMessage = message;
        StatusIsError = isError;
    }
}
