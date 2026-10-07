namespace BlueHeighliner.Beacon.Views;

/// <summary>The application window; starts its view model once opened.</summary>
internal sealed partial class MainWindow : Window
{
    /// <summary>Initializes a new instance of the <see cref="MainWindow" /> class.</summary>
    public MainWindow()
    {
        InitializeComponent();
        Opened += async (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                await vm.Start();
            }
        };
    }
}
