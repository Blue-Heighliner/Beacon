namespace BlueHeighliner.Beacon.Tests.Unit.Data;

public sealed class DodIdHasherTests
{
    private readonly DodIdHasher hasher = new();

    [Fact]
    public void Hash_IsDeterministicUppercaseHex()
    {
        string hash = hasher.Hash("1234567890");

        Assert.Equal(hasher.Hash("1234567890"), hash);
        Assert.Equal(64, hash.Length);
        Assert.Equal(hash.ToUpperInvariant(), hash);
    }

    [Fact]
    public void Hash_DiffersPerValue() => Assert.NotEqual(hasher.Hash("1234567890"), hasher.Hash("1234567891"));

    [Fact]
    public void Hash_OfEmpty_IsEmpty() => Assert.Equal("", hasher.Hash(""));
}
