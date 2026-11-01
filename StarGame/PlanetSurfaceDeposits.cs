using Raylib_cs;

namespace StarflightGame;

public readonly struct SurfaceMineralDeposit
{
    public string Name { get; init; }
    public int Quantity { get; init; }
    public Color Color { get; init; }
    public int TileX { get; init; }
    public int TileY { get; init; }
}

/// <summary>
/// Classic Starflight surface outcroppings: minerals sit on the terrain, denser at high altitude,
/// never in liquid. Drive the terrain vehicle over one to pick it up.
/// </summary>
public sealed class PlanetSurfaceDeposits
{
    private readonly Planet _planet;
    private readonly MineralTradeEntry[] _catalog;
    private readonly int _mineralPercent;
    private readonly HashSet<long> _collected = new HashSet<long>();

    public string PlanetName => _planet.Name;

    private PlanetSurfaceDeposits(Planet planet, MineralTradeEntry[] catalog, int mineralPercent)
    {
        _planet = planet;
        _catalog = catalog;
        _mineralPercent = mineralPercent;
    }

    public static PlanetSurfaceDeposits Create(Planet planet, IReadOnlyList<MineralTradeEntry> tradeList)
    {
        var catalog = new List<MineralTradeEntry>();
        for (int i = 0; i < tradeList.Count; i++)
        {
            MineralTradeEntry entry = tradeList[i];
            if (string.IsNullOrWhiteSpace(entry.Name)
                || string.Equals(entry.Name, "Endurium", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            catalog.Add(entry);
        }

        return new PlanetSurfaceDeposits(planet, catalog.ToArray(), MineralPercentFor(planet));
    }

    public bool TryGet(int tileX, int tileY, out SurfaceMineralDeposit deposit)
    {
        deposit = default;
        if (_catalog.Length == 0 || _collected.Contains(TileKey(tileX, tileY)))
        {
            return false;
        }

        int hash = PlanetGenerator.StableHash($"{_planet.Name}|outcrop|{tileX}|{tileY}");
        float roll = (hash & 0xFFFF) / 65535f;
        float maxChance = _mineralPercent / 100f * 0.007f;
        if (roll > maxChance)
        {
            return false;
        }

        float lon = WrapLon((tileX + 0.5f) * Views.PlanetSurfaceView.DegreesPerTile);
        float lat = Math.Clamp((tileY + 0.5f) * Views.PlanetSurfaceView.DegreesPerTile, -89.5f, 89.5f);
        _planet.SampleSurface(lon, lat, out _, out float height, out bool liquid);
        if (liquid)
        {
            return false;
        }

        float altitudeChance = 0.18f + 1.82f * height * height;
        if (roll > maxChance * altitudeChance)
        {
            return false;
        }

        MineralTradeEntry mineral = PickMineral(hash);
        deposit = new SurfaceMineralDeposit
        {
            Name = mineral.Name,
            Quantity = 1,
            Color = ColorForPrice(mineral.Price),
            TileX = tileX,
            TileY = tileY
        };
        return true;
    }

    public bool TryFindAt(float longitude, float latitude, out SurfaceMineralDeposit deposit)
    {
        int tileX = (int)MathF.Floor(longitude / Views.PlanetSurfaceView.DegreesPerTile);
        int tileY = (int)MathF.Floor(latitude / Views.PlanetSurfaceView.DegreesPerTile);

        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (TryGet(tileX + dx, tileY + dy, out deposit))
                {
                    return true;
                }
            }
        }

        deposit = default;
        return false;
    }

    public void Collect(int tileX, int tileY)
    {
        _collected.Add(TileKey(tileX, tileY));
    }

    private MineralTradeEntry PickMineral(int hash)
    {
        int totalWeight = 0;
        for (int i = 0; i < _catalog.Length; i++)
        {
            totalWeight += MineralWeight(_catalog[i].Price);
        }

        int pick = Math.Abs(hash >> 16) % Math.Max(1, totalWeight);
        int cumulative = 0;
        for (int i = 0; i < _catalog.Length; i++)
        {
            cumulative += MineralWeight(_catalog[i].Price);
            if (pick < cumulative)
            {
                return _catalog[i];
            }
        }

        return _catalog[_catalog.Length - 1];
    }

    private static int MineralWeight(int price)
    {
        return Math.Max(1, 480 - price);
    }

    private static Color ColorForPrice(int price)
    {
        if (price >= 380)
        {
            return new Color(220, 230, 255, 255);
        }

        if (price >= 300)
        {
            return new Color(240, 200, 70, 255);
        }

        if (price >= 200)
        {
            return new Color(200, 200, 210, 255);
        }

        if (price >= 120)
        {
            return new Color(230, 140, 50, 255);
        }

        return new Color(180, 80, 40, 255);
    }

    private static int MineralPercentFor(Planet planet)
    {
        return planet.Composition switch
        {
            PlanetComposition.Molten => 70,
            PlanetComposition.GasGiant => 0,
            PlanetComposition.IceGiant => 0,
            _ => planet.TerrainStyle switch
            {
                PlanetTerrainStyle.Rocky => 48,
                PlanetTerrainStyle.Desert => 38,
                PlanetTerrainStyle.Volcanic => 60,
                PlanetTerrainStyle.Ice => 18,
                PlanetTerrainStyle.Oceanic => 22,
                PlanetTerrainStyle.Lush => 28,
                _ => 30
            }
        };
    }

    private static long TileKey(int tileX, int tileY)
    {
        return ((long)tileX << 32) ^ (uint)tileY;
    }

    private static float WrapLon(float lon)
    {
        while (lon < 0f)
        {
            lon += 360f;
        }

        while (lon >= 360f)
        {
            lon -= 360f;
        }

        return lon;
    }
}
