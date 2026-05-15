namespace StarflightGame;

/// <summary>
/// A tradeable mineral and its standard buy/sell reference price in credits (from embedded data).
/// </summary>
public sealed class MineralTradeEntry
{
    public string Name { get; init; } = "";

    public int Price { get; init; }
}
