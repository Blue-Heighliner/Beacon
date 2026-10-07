namespace BlueHeighliner.Beacon.Tests.Integration.Data;

public sealed class ItemRepositoryTests : IDisposable
{
    private readonly DataFixture fixture = new();
    private readonly ItemRepository items;

    public ItemRepositoryTests() => items = new ItemRepository(fixture.Database);

    public void Dispose() => fixture.Dispose();

    [Fact]
    public async Task TryInsert_StoresItem_AndRejectsDuplicateSerial()
    {
        Assert.True(await items.TryInsert(New("SN1", "Comms", "bob", 1)));
        Assert.False(await items.TryInsert(New("SN1", "Other", "alice", 2)));

        Assert.Single(await items.Query());
    }

    [Fact]
    public async Task Query_ReturnsNewestFirst_WithRoundTrippedFields()
    {
        await items.TryInsert(New("SN1", "Comms", "bob", 1));
        await items.TryInsert(New("SN2", "Comms", "bob", 2));

        List<InventoryItem> all = await items.Query();

        Assert.Equal(["SN2", "SN1"], all.Select(i => i.SerialNumber));
        Assert.Equal(New("SN2", "Comms", "bob", 2) with { Id = all[0].Id }, all[0]);
    }

    [Fact]
    public async Task Query_FiltersByCategoryUserAndDateRange()
    {
        await items.TryInsert(New("SN1", "Comms", "bob", 1));
        await items.TryInsert(New("SN2", "IT", "bob", 2));
        await items.TryInsert(New("SN3", "IT", "alice", 3));

        Assert.Equal(["SN3", "SN2"], (await items.Query(category: "IT")).Select(i => i.SerialNumber));
        Assert.Equal(["SN2", "SN1"], (await items.Query(insertedBy: "bob")).Select(i => i.SerialNumber));
        Assert.Equal(["SN2"], (await items.Query(fromUtc: Day(2), toUtcExclusive: Day(3))).Select(i => i.SerialNumber));
    }

    [Fact]
    public async Task Delete_RemovesItem()
    {
        await items.TryInsert(New("SN1", "Comms", "bob", 1));
        long id = (await items.Query()).Single().Id;

        await items.Delete(id);

        Assert.Empty(await items.Query());
    }

    [Fact]
    public async Task GetCategoriesAndUsers_AreDistinctSortedAndNonEmpty()
    {
        await items.TryInsert(New("SN1", "Comms", "bob", 1));
        await items.TryInsert(New("SN2", "Comms", "alice", 2));
        await items.TryInsert(New("SN3", "", "alice", 3));

        Assert.Equal(["Comms"], await items.GetCategories());
        Assert.Equal(["alice", "bob"], await items.GetInsertingUsers());
    }

    private static DateTime Day(int day) => new(2024, 1, day, 0, 0, 0, DateTimeKind.Utc);

    private static InventoryItem New(string serial, string category, string insertedBy, int day) => new() { SerialNumber = serial, ItemName = "Item " + serial, Category = category, InsertedBy = insertedBy, CreatedAt = Day(day) };
}
