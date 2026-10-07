namespace BlueHeighliner.Beacon.Tests.Unit.Services;

public sealed class SmallServicesTests
{
    [Fact]
    public void Clock_ReturnsCurrentUtcTime()
    {
        DateTime before = DateTime.UtcNow;

        DateTime now = new Clock().UtcNow;

        Assert.InRange(now, before, DateTime.UtcNow);
        Assert.Equal(DateTimeKind.Utc, now.Kind);
    }

    [Fact]
    public void AppPaths_PlacesFilesInTheDataDirectory()
    {
        AppPaths paths = new(Path.Combine("data", "dir"));

        Assert.Equal(Path.Combine("data", "dir"), paths.DataDirectory);
        Assert.Equal(Path.Combine("data", "dir", "inventory.db"), paths.DatabasePath);
        Assert.Equal(Path.Combine("data", "dir", "dodid.key"), paths.KeyPath);
    }

    [AvaloniaFact]
    public async Task FilePicker_WithoutDesktopWindow_ReturnsNull() => Assert.Null(await new FilePicker().PickExcelSavePath("x.xlsx"));

    [AvaloniaFact]
    public async Task UiTimer_TicksOnceAfterTheIntervalAndCanBeRestarted()
    {
        int ticks = 0;
        IUiTimer timer = new UiTimerFactory().Create(TimeSpan.FromMilliseconds(50), () => ticks++);

        timer.Restart();
        timer.Restart();
        await Task.Delay(300);

        Assert.Equal(1, ticks);
    }
}
