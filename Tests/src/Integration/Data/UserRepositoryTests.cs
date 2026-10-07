namespace BlueHeighliner.Beacon.Tests.Integration.Data;

public sealed class UserRepositoryTests : IDisposable
{
    private readonly DataFixture fixture = new();
    private readonly UserRepository users;

    public UserRepositoryTests() => users = new UserRepository(fixture.Database, fixture.Cipher);

    public void Dispose() => fixture.Dispose();

    [Fact]
    public async Task Create_ThenLookup_ByEveryKey()
    {
        await users.Create(New("Alice", "1111111111", barcodeHash: "BADGE"));

        Assert.Equal("Alice", (await users.GetByUsername("alice"))?.Username);
        Assert.Equal("Alice", (await users.GetByBarcodeHash("BADGE"))?.Username);
        Assert.Equal("1111111111", (await users.GetByDodId("1111111111"))?.DodId);
        Assert.Null(await users.GetByUsername("nobody"));
    }

    [Fact]
    public async Task Create_RoundTripsFields()
    {
        User created = New("Alice", "1111111111", barcodeHash: "BADGE") with { PasswordHash = "hash", PasswordSalt = "salt", IsAdmin = true };
        await users.Create(created);

        User? loaded = await users.GetByUsername("Alice");

        Assert.NotNull(loaded);
        Assert.True(loaded.Id > 0);
        Assert.Equal(created with { Id = loaded.Id }, loaded);
    }

    [Fact]
    public async Task Update_PersistsChanges()
    {
        await users.Create(New("Alice", "1111111111"));
        User stored = (await users.GetByUsername("Alice"))!;

        await users.Update(stored with { Username = "Alicia", DodId = "2222222222", IsAdmin = true });

        User? loaded = await users.GetByDodId("2222222222");
        Assert.Equal("Alicia", loaded?.Username);
        Assert.True(loaded?.IsAdmin);
        Assert.Null(await users.GetByDodId("1111111111"));
    }

    [Fact]
    public async Task Delete_RemovesUser()
    {
        await users.Create(New("Alice", "1111111111"));
        User stored = (await users.GetByUsername("Alice"))!;

        await users.Delete(stored.Id);

        Assert.Empty(await users.GetAll());
    }

    [Fact]
    public async Task GetAll_IsOrderedByUsername()
    {
        await users.Create(New("Bob", "2222222222"));
        await users.Create(New("Alice", "1111111111"));

        Assert.Equal(["Alice", "Bob"], (await users.GetAll()).Select(u => u.Username));
    }

    [Fact]
    public async Task CountAdmins_CountsOnlyAdmins()
    {
        await users.Create(New("Alice", "1111111111") with { IsAdmin = true });
        await users.Create(New("Bob", "2222222222"));

        Assert.Equal(1, await users.CountAdmins());
    }

    [Fact]
    public async Task Create_RejectsDuplicateDodId()
    {
        await users.Create(New("Alice", "1111111111"));

        await Assert.ThrowsAsync<SqliteException>(() => users.Create(New("Bob", "1111111111")));
    }

    private static User New(string username, string dodId, string? barcodeHash = null) => new() { Username = username, DodId = dodId, BarcodeHash = barcodeHash, CreatedAt = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc) };
}
