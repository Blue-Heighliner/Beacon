namespace BlueHeighliner.Beacon.Tests.Integration.Services;

public sealed class ExcelExportServiceTests
{
    [Fact]
    public async Task Export_WritesHeaderAndRows()
    {
        using TempDirectory directory = new();
        string path = directory.File("out.xlsx");
        InventoryItem item = new() { SerialNumber = "SN1", ItemName = "Radio", Category = "Comms", InsertedBy = "bob", CreatedAt = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc) };

        await new ExcelExportService().Export(path, [item]);

        using XLWorkbook workbook = new(path);
        IXLWorksheet sheet = workbook.Worksheet("Inventory");
        Assert.Equal("Serial Number", sheet.Cell(1, 1).GetString());
        Assert.Equal("SN1", sheet.Cell(2, 1).GetString());
        Assert.Equal("Radio", sheet.Cell(2, 2).GetString());
        Assert.Equal("Comms", sheet.Cell(2, 3).GetString());
        Assert.Equal("bob", sheet.Cell(2, 4).GetString());
        Assert.Equal(item.CreatedAtLocal, sheet.Cell(2, 5).GetDateTime());
    }
}
