using Avalonia.Controls;

namespace Artemis.Views;

public partial class UserManagementView : UserControl
{
    public UserManagementView()
    {
        InitializeComponent();
        // Executed from code-behind: command bindings inside a Flyout popup don't reliably
        // resolve the view's DataContext.
        ConfirmDeleteUserButton.Click += (_, _) =>
        {
            DeleteUserButton.Flyout?.Hide();
            if (DataContext is ViewModels.UserManagementViewModel vm)
                vm.DeleteSelectedUserCommand.Execute(null);
        };
    }
}
