using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Artemis.ViewModels;

namespace Artemis.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();

        // Barcode scanners type the code and send Enter.
        BadgeBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter && DataContext is LoginViewModel vm)
            {
                vm.BadgeLoginCommand.Execute(null);
                e.Handled = true;
            }
        };

        PasswordBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter && DataContext is LoginViewModel vm)
            {
                vm.LoginCommand.Execute(null);
                e.Handled = true;
            }
        };
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        BadgeBox.Focus();
    }
}
