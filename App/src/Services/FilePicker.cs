namespace BlueHeighliner.Beacon.Services;

/// <summary>Asks the user where to save files.</summary>
internal interface IFilePicker
{
    /// <summary>Shows a save dialog for an Excel workbook.</summary>
    /// <param name="suggestedFileName">The file name to pre-fill.</param>
    /// <returns>The chosen local path, or null if the user cancelled or no window is available.</returns>
    Task<string?> PickExcelSavePath(string suggestedFileName);
}

internal sealed class FilePicker : IFilePicker
{
    public async Task<string?> PickExcelSavePath(string suggestedFileName)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
        {
            return null;
        }

        IStorageFile? file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Inventory to Excel",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "xlsx",
            FileTypeChoices = [new FilePickerFileType("Excel Workbook") { Patterns = ["*.xlsx"] }],
        });
        return file?.Path.LocalPath;
    }
}
