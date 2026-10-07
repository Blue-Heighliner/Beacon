namespace BlueHeighliner.Beacon.Views;

/// <summary>Administrator user management screen.</summary>
internal sealed partial class UserManagementView : UserControl
{
    /// <summary>Initializes a new instance of the <see cref="UserManagementView" /> class.</summary>
    public UserManagementView()
    {
        InitializeComponent();

        // Executed from code-behind: command bindings inside a Flyout popup don't reliably
        // resolve the view's DataContext.
        ConfirmDeleteUserButton.Click += (_, _) =>
        {
            DeleteUserButton.Flyout?.Hide();
            if (DataContext is UserManagementViewModel vm)
            {
                vm.DeleteSelectedUserCommand.Execute(null);
            }
        };
    }
}
