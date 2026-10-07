namespace BlueHeighliner.Beacon.Services;

/// <summary>Supplies the current time so time-dependent code can be tested.</summary>
internal interface IClock
{
    /// <summary>Gets the current UTC time.</summary>
    DateTime UtcNow { get; }
}

internal sealed class Clock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
