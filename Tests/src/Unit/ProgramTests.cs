namespace BlueHeighliner.Beacon.Tests.Unit;

public sealed class ProgramTests
{
    [Fact]
    public void BuildAvaloniaApp_ReturnsConfiguredBuilder() => Assert.NotNull(Program.BuildAvaloniaApp());
}
