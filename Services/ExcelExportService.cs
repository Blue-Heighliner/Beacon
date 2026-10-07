using System.Collections.Generic;
using Artemis.Models;
using ClosedXML.Excel;

namespace Artemis.Services;

public static class ExcelExportService
{
    public static void Export(string filePath, IEnumerable<InventoryItem> items)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Inventory");

        sheet.Cell(1, 1).Value = "Serial Number";
        sheet.Cell(1, 2).Value = "Item Name";
        sheet.Cell(1, 3).Value = "Category";
        sheet.Cell(1, 4).Value = "Inserted By";
        sheet.Cell(1, 5).Value = "Created At";
        sheet.Row(1).Style.Font.Bold = true;

        var row = 2;
        foreach (var item in items)
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
        workbook.SaveAs(filePath);
    }
}
