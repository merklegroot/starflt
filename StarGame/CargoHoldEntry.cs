namespace StarflightGame;

/// <summary>
/// A single line item in the ship's cargo hold manifest.
/// </summary>
public sealed class CargoHoldEntry
{
    public string Name { get; init; } = "";

    public int Quantity { get; set; }

    public string Category { get; init; } = "";
}
