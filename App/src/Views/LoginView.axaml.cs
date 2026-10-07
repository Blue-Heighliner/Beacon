namespace BlueHeighliner.Beacon.Views;

/// <summary>Sign-in and registration screen.</summary>
internal sealed partial class LoginView : UserControl
{
    /// <summary>Initializes a new instance of the <see cref="LoginView" /> class.</summary>
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
