namespace BlueHeighliner.Beacon.Tests.Unit.Services;

public sealed class ThemeServiceTests
{
    [Fact]
    public void Toggle_WithoutApplication_DoesNothing()
    {
        ThemeService theme = new();

        theme.Toggle();

        Assert.False(theme.IsDark);
    }
}
