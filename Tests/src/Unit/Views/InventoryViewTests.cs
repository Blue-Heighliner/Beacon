namespace BlueHeighliner.Beacon.Tests.Unit.Views;

public sealed class InventoryViewTests
{
    private readonly Mock<IAuthService> auth = new();
    private readonly Mock<IItemRepository> items = new();
    private readonly Mock<IExcelExportService> excel = new();
    private readonly InventoryItem existing = new() { Id = 3, SerialNumber = "SN1", ItemName = "Radio", Category = "Comms", InsertedBy = "bob", CreatedAt = DateTime.UtcNow };
    private readonly Mock<IFilePicker> picker = new();
    private readonly Mock<INavigation> navigation = new();

    public InventoryViewTests()
    {
        auth.Setup(x => x.CurrentUser).Returns(new User { Id = 1, Username = "bob", DodId = "1234567890", CreatedAt = DateTime.UtcNow });
        items.Setup(x => x.Query(null, null, null, null, default)).ReturnsAsync([existing]);
        items.Setup(x => x.GetCategories(default)).ReturnsAsync(["Comms"]);
        items.Setup(x => x.GetInsertingUsers(default)).ReturnsAsync(["bob"]);
        items.Setup(x => x.TryInsert(It.IsAny<InventoryItem>(), default)).ReturnsAsync(true);
    }

    [AvaloniaFact]
    public void Loaded_FocusesSerialBox()
    {
        (_, InventoryView view, _) = Show();

        Assert.True(view.SerialBox.IsFocused);
    }

    [AvaloniaFact]
    public void EnterInSerialBox_WithText_MovesToItemName()
    {
        (Window window, InventoryView view, _) = Show();
        view.SerialBox.Text = "SN2";

        Press(window, Key.Enter);

        Assert.True(view.ItemNameBox.IsFocused);
    }

    [AvaloniaFact]
    public void EnterInSerialBox_WhenBlank_StaysPut()
    {
        (Window window, InventoryView view, _) = Show();

        Press(window, Key.Enter);

        Assert.True(view.SerialBox.IsFocused);
    }

    [AvaloniaFact]
    public void OtherKeyInSerialBox_DoesNotMove()
    {
        (Window window, InventoryView view, _) = Show();
        view.SerialBox.Text = "SN2";

        Press(window, Key.A);

        Assert.True(view.SerialBox.IsFocused);
    }

    [AvaloniaFact]
    public void EnterInItemNameBox_MovesToCategory()
    {
        (Window window, InventoryView view, _) = Show();
        view.ItemNameBox.Focus();

        Press(window, Key.Enter);

        Assert.True(view.CategoryBox.IsKeyboardFocusWithin);
    }

    [AvaloniaFact]
    public void OtherKeyInItemNameBox_DoesNotMove()
    {
        (Window window, InventoryView view, _) = Show();
        view.ItemNameBox.Focus();

        Press(window, Key.A);

        Assert.True(view.ItemNameBox.IsFocused);
    }

    [AvaloniaFact]
    public void EnterInCategoryBox_AddsItemAndRefocusesSerial()
    {
        (Window window, InventoryView view, InventoryViewModel viewModel) = Show();
        viewModel.SerialInput = "SN2";
        view.CategoryBox.Focus();

        Press(window, Key.Enter);

        items.Verify(x => x.TryInsert(It.Is<InventoryItem>(i => i.SerialNumber == "SN2"), default), Times.Once);
        Assert.True(view.SerialBox.IsFocused);
    }

    [AvaloniaFact]
    public void OtherKeyInCategoryBox_Ignored()
    {
        (Window window, InventoryView view, _) = Show();
        view.CategoryBox.Focus();

        Press(window, Key.A);

        items.Verify(x => x.TryInsert(It.IsAny<InventoryItem>(), default), Times.Never);
    }

    [AvaloniaFact]
    public void ConfirmDelete_DeletesSelectedItem()
    {
        (_, InventoryView view, InventoryViewModel viewModel) = Show();
        viewModel.SelectedItem = existing;

        view.ConfirmDeleteButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        items.Verify(x => x.Delete(3, default), Times.Once);
    }

    [AvaloniaFact]
    public void ConfirmLogout_LogsOut_AndCancelDoesNot()
    {
        (_, InventoryView view, _) = Show();

        view.CancelLogoutButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        navigation.Verify(x => x.ShowLogin(), Times.Never);

        view.ConfirmLogoutButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        navigation.Verify(x => x.ShowLogin(), Times.Once);
    }

    [AvaloniaFact]
    public void ExportButtons_RunTheExportCommands()
    {
        picker.Setup(x => x.PickExcelSavePath(It.IsAny<string>())).ReturnsAsync("out.xlsx");
        (_, InventoryView view, _) = Show();

        view.ExportFilteredButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        view.ExportAllButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        picker.Verify(x => x.PickExcelSavePath(It.IsAny<string>()), Times.Exactly(2));
    }

    [AvaloniaFact]
    public void ButtonHandlers_WithoutViewModel_DoNothing()
    {
        InventoryView view = new();
        Window window = new() { Content = view };
        window.Show();

        view.ConfirmDeleteButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        view.ConfirmLogoutButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        view.ExportAllButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        view.ExportFilteredButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        view.CategoryBox.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter, Source = view.CategoryBox });

        Assert.Null(view.DataContext);
    }

    private static void Press(Window window, Key key) => window.KeyPress(key, RawInputModifiers.None, key == Key.Enter ? PhysicalKey.Enter : PhysicalKey.A, key == Key.Enter ? null : key.ToString().ToLowerInvariant());

    private (Window Window, InventoryView View, InventoryViewModel ViewModel) Show()
    {
        InventoryViewModel viewModel = new(auth.Object, items.Object, Mock.Of<IThemeService>(), Mock.Of<IInputValidator>(), excel.Object, picker.Object, Mock.Of<IClock>(), Mock.Of<IUiTimerFactory>(x => x.Create(It.IsAny<TimeSpan>(), It.IsAny<Action>()) == Mock.Of<IUiTimer>()), navigation.Object);
        viewModel.Load().GetAwaiter().GetResult();
        InventoryView view = new() { DataContext = viewModel };
        Window window = new() { Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, view, viewModel);
    }
}
