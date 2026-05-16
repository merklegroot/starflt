namespace StarflightGame;

/// <summary>
/// Initial ship cargo hold contents and capacity, loaded from embedded data.
/// </summary>
public sealed class CargoManifest
{
    public int Capacity { get; init; }

    public int FuelCapacity { get; init; }

    public IReadOnlyList<CargoHoldEntry> Items { get; init; } = Array.Empty<CargoHoldEntry>();
}
