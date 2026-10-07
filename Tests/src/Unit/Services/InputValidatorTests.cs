namespace BlueHeighliner.Beacon.Tests.Unit.Services;

public sealed class InputValidatorTests
{
    private readonly InputValidator validator = new();

    [Theory]
    [InlineData("a--b")]
    [InlineData("a/*b")]
    [InlineData("a;b")]
    [InlineData("x' OR 1=1")]
    [InlineData("1 UNION SELECT 2")]
    [InlineData("drop table Users")]
    [InlineData("delete from Items")]
    [InlineData("insert into Items")]
    [InlineData("x OR 'a'='a'")]
    [InlineData("xp_cmdshell")]
    public void IsSuspicious_ReturnsTrue_ForInjectionMarkers(string input) => Assert.True(validator.IsSuspicious(input));

    [Theory]
    [InlineData("SN-SELECT-01")]
    [InlineData("Radio, Handheld")]
    [InlineData("")]
    [InlineData(null)]
    public void IsSuspicious_ReturnsFalse_ForLegitimateInput(string? input) => Assert.False(validator.IsSuspicious(input));

    [Fact]
    public void IsSuspicious_ReturnsTrue_WhenAnyInputIsSuspicious() => Assert.True(validator.IsSuspicious("fine", null, "a;b"));

    [Fact]
    public void RejectionMessage_IsNotEmpty() => Assert.False(string.IsNullOrEmpty(validator.RejectionMessage));
}
