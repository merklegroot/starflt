namespace StarflightGame;

/// <summary>
/// A tradeable mineral and its reference value in MU (monetary units), from embedded data (manual mineral chart).
/// </summary>
public sealed class MineralTradeEntry
{
    public string Name { get; init; } = "";

    public int Price { get; init; }
}
