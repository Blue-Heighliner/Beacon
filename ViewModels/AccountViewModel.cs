using System;
using Artemis.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Artemis.ViewModels;

public partial class AccountViewModel : ViewModelBase
{
    private readonly AuthService _auth;
    private readonly Action _onBack;

    [ObservableProperty] private string _currentPassword = "";
    [ObservableProperty] private string _newPassword = "";
    [ObservableProperty] private string _newPasswordConfirm = "";
    [ObservableProperty] private string _badgeInput = "";

    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool _statusIsError;

    public string Username => _auth.CurrentUser?.Username ?? "";
    public string DodId => _auth.CurrentUser?.DodId ?? "";
    public bool HasBadge => _auth.CurrentUser?.BarcodeHash is not null;

    public AccountViewModel(AuthService auth, Action onBack)
    {
        _auth = auth;
        _onBack = onBack;
    }

    [RelayCommand]
    private void ChangePassword()
    {
        if (NewPassword != NewPasswordConfirm)
        {
            ShowStatus("New passwords do not match.", isError: true);
            return;
        }

        var (success, error) = _auth.ChangePassword(CurrentPassword, NewPassword);
        if (!success)
        {
            ShowStatus(error, isError: true);
            return;
        }

        CurrentPassword = NewPassword = NewPasswordConfirm = "";
        ShowStatus("Password changed.", isError: false);
    }

    [RelayCommand]
    private void ChangeBadge()
    {
        var (success, error) = _auth.ChangeBadge(BadgeInput);
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
    private void Back() => _onBack();

    private void ShowStatus(string message, bool isError)
    {
        StatusMessage = message;
        StatusIsError = isError;
    }
}
