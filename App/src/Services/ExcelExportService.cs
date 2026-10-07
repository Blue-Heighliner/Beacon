namespace BlueHeighliner.Beacon.Services;

/// <summary>Writes inventory items to an Excel workbook.</summary>
internal interface IExcelExportService
{
    /// <summary>Writes the items to a single-sheet workbook.</summary>
    /// <param name="filePath">The .xlsx file to create or overwrite.</param>
    /// <param name="items">The items to export, in row order.</param>
    /// <param name="cancellation">Cancels the write.</param>
    Task Export(string filePath, IEnumerable<InventoryItem> items, CancellationToken cancellation = default);
}

internal sealed class ExcelExportService : IExcelExportService
{
    public async Task Export(string filePath, IEnumerable<InventoryItem> items, CancellationToken cancellation = default)
    {
        using XLWorkbook workbook = new();
        IXLWorksheet sheet = workbook.Worksheets.Add("Inventory");

        sheet.Cell(1, 1).Value = "Serial Number";
        sheet.Cell(1, 2).Value = "Item Name";
        sheet.Cell(1, 3).Value = "Category";
        sheet.Cell(1, 4).Value = "Inserted By";
        sheet.Cell(1, 5).Value = "Created At";
        sheet.Row(1).Style.Font.Bold = true;

        int row = 2;
        foreach (InventoryItem item in items)
        {
            sheet.Cell(row, 1).Value = item.SerialNumber;
            sheet.Cell(row, 2).Value = item.ItemName;
            sheet.Cell(row, 3).Value = item.Category;
            sheet.Cell(row, 4).Value = item.InsertedBy;
            sheet.Cell(row, 5).Value = item.CreatedAtLocal;
            sheet.Cell(row, 5).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
            row++;
        }

        sheet.Columns().AdjustToContents();

        using MemoryStream buffer = new();
        workbook.SaveAs(buffer);
        buffer.Position = 0;
        await using FileStream file = File.Create(filePath);
        await buffer.CopyToAsync(file, cancellation);
    }
}
