namespace BlueHeighliner.Beacon.Tests.Unit;

public sealed class ViewLocatorTests
{
    private readonly ViewLocator locator = new();

    [Fact]
    public void Match_AcceptsOnlyViewModels()
    {
        Assert.True(locator.Match(new AccountViewModel(Mock.Of<IAuthService>(), Mock.Of<INavigation>())));
        Assert.False(locator.Match("not a view model"));
        Assert.False(locator.Match(null));
    }

    [Fact]
    public void Build_WithNull_ReturnsNull() => Assert.Null(locator.Build(null));

    [AvaloniaFact]
    public void Build_ReturnsMatchingView() => Assert.IsType<AccountView>(locator.Build(new AccountViewModel(Mock.Of<IAuthService>(), Mock.Of<INavigation>())));

    [AvaloniaFact]
    public void Build_WithoutMatchingView_ReturnsNotFoundText()
    {
        TextBlock text = Assert.IsType<TextBlock>(locator.Build(new OrphanViewModel()));

        Assert.StartsWith("Not Found: ", text.Text);
    }

    private sealed class OrphanViewModel : ViewModelBase;
}
