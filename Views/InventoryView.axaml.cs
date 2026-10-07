using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Artemis.Services;
using Artemis.ViewModels;

namespace Artemis.Views;

public partial class InventoryView : UserControl
{
    public InventoryView()
    {
        InitializeComponent();

        // Barcode scanners type the serial and send Enter. Rather than committing there and
        // then -- which saved every scan with an empty name and category -- Enter walks
        // forward through the entry fields, and only the last one commits. Existing text is
        // selected on arrival so typing replaces it while a bare Enter keeps it, which is what
        // makes batch scanning a shelf of identical gear still fast.
        SerialBox.KeyDown += (_, e) =>
        {
            if (e.Key != Key.Enter)
                return;

            e.Handled = true;
            if (string.IsNullOrWhiteSpace(SerialBox.Text))
                return;

            ItemNameBox.Focus();
            ItemNameBox.SelectAll();
        };

        ItemNameBox.KeyDown += (_, e) =>
        {
            if (e.Key != Key.Enter)
                return;

            e.Handled = true;
            CategoryBox.Focus();
            // AutoCompleteBox has no SelectAll of its own; reach the TextBox in its template.
            CategoryBox.FindDescendantOfType<TextBox>()?.SelectAll();
        };

        // handledEventsToo: the AutoCompleteBox marks Enter handled when its suggestion list
        // is open, which would otherwise swallow the commit.
        CategoryBox.AddHandler(KeyDownEvent, OnCategoryKeyDown, handledEventsToo: true);

        ExportFilteredButton.Click += async (_, _) => await ExportAsync(filteredOnly: true);
        ExportAllButton.Click += async (_, _) => await ExportAsync(filteredOnly: false);
        // Executed from code-behind: command bindings inside a Flyout popup don't reliably
        // resolve the view's DataContext.
        ConfirmDeleteButton.Click += (_, _) =>
        {
            DeleteButton.Flyout?.Hide();
            if (DataContext is InventoryViewModel vm)
                vm.DeleteSelectedItemCommand.Execute(null);
        };

        CancelLogoutButton.Click += (_, _) => LogoutButton.Flyout?.Hide();
        ConfirmLogoutButton.Click += (_, _) =>
        {
            LogoutButton.Flyout?.Hide();
            if (DataContext is InventoryViewModel vm)
                vm.LogoutCommand.Execute(null);
        };
    }

    private void OnCategoryKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || DataContext is not InventoryViewModel vm)
            return;

        e.Handled = true;

        // With suggestions showing, this Enter accepts the highlighted one; the next commits.
        if (CategoryBox.IsDropDownOpen)
        {
            CategoryBox.IsDropDownOpen = false;
            return;
        }

        vm.AddItemCommand.Execute(null);
        SerialBox.Focus();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        SerialBox.Focus();
    }

    private async Task ExportAsync(bool filteredOnly)
    {
        if (DataContext is not InventoryViewModel vm)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Inventory to Excel",
            SuggestedFileName = $"Inventory-{DateTime.Now:yyyy-MM-dd}.xlsx",
            DefaultExtension = "xlsx",
            FileTypeChoices = [new FilePickerFileType("Excel Workbook") { Patterns = ["*.xlsx"] }],
        });
        if (file is null)
            return;

        var items = filteredOnly ? vm.GetFilteredItems() : vm.GetAllItems();
        ExcelExportService.Export(file.Path.LocalPath, items);
        vm.NotifyExported(file.Path.LocalPath, items.Count);
    }
}
