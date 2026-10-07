using System;
using Artemis.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Artemis.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly AuthService _auth;
    private readonly Action _onLoginSucceeded;

    // Sign in
    [ObservableProperty] private string _username = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string _badgeInput = "";

    // Register
    [ObservableProperty] private string _regUsername = "";
    [ObservableProperty] private string _regDodId = "";
    [ObservableProperty] private string _regPassword = "";
    [ObservableProperty] private string _regPasswordConfirm = "";
    [ObservableProperty] private string _regBadge = "";

    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool _statusIsError;

    public LoginViewModel(AuthService auth, Action onLoginSucceeded)
    {
        _auth = auth;
        _onLoginSucceeded = onLoginSucceeded;
    }

    [RelayCommand]
    private void Login()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrEmpty(Password))
        {
            ShowError("Enter your username and password.");
            return;
        }

        if (_auth.LoginWithPassword(Username, Password))
            _onLoginSucceeded();
        else
            ShowError("Invalid username or password.");
    }

    [RelayCommand]
    private void BadgeLogin()
    {
        if (string.IsNullOrWhiteSpace(BadgeInput))
            return;

        if (_auth.LoginWithBarcode(BadgeInput))
        {
            _onLoginSucceeded();
        }
        else
        {
            BadgeInput = "";
            ShowError("Badge not recognized.");
        }
    }

    [RelayCommand]
    private void Register()
    {
        if (!string.IsNullOrEmpty(RegPassword) && RegPassword != RegPasswordConfirm)
        {
            ShowError("Passwords do not match.");
            return;
        }

        var (success, error) = _auth.Register(RegUsername, RegPassword, RegBadge, RegDodId);
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
    private void ToggleTheme() => ThemeService.Toggle();

    private void ShowError(string message)
    {
        StatusMessage = message;
        StatusIsError = true;
    }
}
