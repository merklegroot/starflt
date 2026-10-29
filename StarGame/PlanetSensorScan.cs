using System.Numerics;
using Raylib_cs;

namespace StarflightGame;

public enum SensorContactKind
{
    Life,
    Mineral
}

public readonly struct SensorContact
{
    public float X { get; init; }
    public float Y { get; init; }
    public SensorContactKind Kind { get; init; }
    public Color Color { get; init; }
}

/// <summary>
/// Orbit sensor readout in the style of classic Starflight (Science Officer → Sensors).
/// </summary>
public sealed class PlanetSensorScan
{
    public const float ScanDurationSeconds = 3.4f;

    public string MassLabel { get; init; } = "";
    public int BioPercent { get; init; }
    public int MineralPercent { get; init; }
    public string Atmosphere { get; init; } = "None";
    public string Hydrosphere { get; init; } = "None";
    public string Lithosphere { get; init; } = "None";
    public Color DiscColor { get; init; }
    public PlanetTerrainStyle TerrainStyle { get; init; }
    public IReadOnlyList<SensorContact> Contacts { get; init; } = Array.Empty<SensorContact>();
    public double ScanStartedAt { get; init; }

    public float ScanProgress
    {
        get
        {
            float elapsed = (float)(Raylib.GetTime() - ScanStartedAt);
            return Math.Clamp(elapsed / ScanDurationSeconds, 0f, 1f);
        }
    }

    public static PlanetSensorScan FromPlanet(Planet planet, IReadOnlyList<MineralTradeEntry>? minerals = null)
    {
        Random rng = new Random(PlanetGenerator.StableHash(planet.Name + "|sensors"));
        float radiusKm = planet.RadiusKm > 1f ? planet.RadiusKm : EstimateRadiusKm(planet);
        double densityKgM3 = DensityKgPerCubicMeter(planet);
        double radiusM = radiusKm * 1000.0;
        double massKg = (4.0 / 3.0) * Math.PI * radiusM * radiusM * radiusM * densityKgM3;
        double massTons = massKg / 1000.0;
        int bio = BioPercentFor(planet, rng);
        int mineral = MineralPercentFor(planet, rng);

        return new PlanetSensorScan
        {
            MassLabel = FormatMassTons(massTons),
            BioPercent = bio,
            MineralPercent = mineral,
            Atmosphere = AtmosphereFor(planet),
            Hydrosphere = HydrosphereFor(planet),
            Lithosphere = LithosphereFor(planet, minerals, rng),
            DiscColor = planet.SurfaceColor,
            TerrainStyle = planet.TerrainStyle,
            Contacts = BuildContacts(planet, bio, mineral, rng),
            ScanStartedAt = Raylib.GetTime()
        };
    }

    private static float EstimateRadiusKm(Planet planet)
    {
        return planet.Composition switch
        {
            PlanetComposition.GasGiant => 55000f,
            PlanetComposition.IceGiant => 24000f,
            PlanetComposition.Molten => 5200f,
            _ => 6000f
        };
    }

    private static double DensityKgPerCubicMeter(Planet planet)
    {
        return planet.Composition switch
        {
            PlanetComposition.GasGiant => 1326,
            PlanetComposition.IceGiant => 1638,
            PlanetComposition.Molten => 5200,
            _ => planet.TerrainStyle == PlanetTerrainStyle.Ice ? 1600 : 5514
        };
    }

    private static string FormatMassTons(double tons)
    {
        if (tons >= 1_000_000)
        {
            return tons.ToString("0.00e+0");
        }

        return $"{tons:N0}";
    }

    private static int BioPercentFor(Planet planet, Random rng)
    {
        return planet.TerrainStyle switch
        {
            PlanetTerrainStyle.Lush => ClampPercent(70 + rng.Next(26)),
            PlanetTerrainStyle.Oceanic => ClampPercent(42 + rng.Next(38)),
            PlanetTerrainStyle.Clouded => ClampPercent(10 + rng.Next(28)),
            PlanetTerrainStyle.Desert => ClampPercent(3 + rng.Next(22)),
            PlanetTerrainStyle.Ice => ClampPercent(rng.Next(16)),
            PlanetTerrainStyle.Rocky => ClampPercent(rng.Next(6)),
            PlanetTerrainStyle.Volcanic => ClampPercent(rng.Next(8)),
            _ => 0
        };
    }

    private static int MineralPercentFor(Planet planet, Random rng)
    {
        return planet.Composition switch
        {
            PlanetComposition.Molten => ClampPercent(55 + rng.Next(36)),
            PlanetComposition.GasGiant => ClampPercent(4 + rng.Next(16)),
            PlanetComposition.IceGiant => ClampPercent(8 + rng.Next(18)),
            _ => planet.TerrainStyle switch
            {
                PlanetTerrainStyle.Rocky => ClampPercent(28 + rng.Next(40)),
                PlanetTerrainStyle.Desert => ClampPercent(22 + rng.Next(35)),
                PlanetTerrainStyle.Volcanic => ClampPercent(40 + rng.Next(40)),
                PlanetTerrainStyle.Ice => ClampPercent(8 + rng.Next(22)),
                _ => ClampPercent(18 + rng.Next(40))
            }
        };
    }

    private static string AtmosphereFor(Planet planet)
    {
        return planet.TerrainStyle switch
        {
            PlanetTerrainStyle.Oceanic => "Nitrogen, Oxygen, Argon",
            PlanetTerrainStyle.Lush => "Nitrogen, Oxygen, Water vapor",
            PlanetTerrainStyle.Desert => "Carbon dioxide, Nitrogen, Argon",
            PlanetTerrainStyle.Rocky => "Trace / none",
            PlanetTerrainStyle.Ice => "Nitrogen, Methane, Carbon dioxide",
            PlanetTerrainStyle.Clouded => "Carbon dioxide, Nitrogen, Sulfur dioxide",
            PlanetTerrainStyle.GasBanded => "Hydrogen, Helium, Methane",
            PlanetTerrainStyle.IceBanded => "Hydrogen, Helium, Methane",
            PlanetTerrainStyle.Volcanic => "Carbon dioxide, Sulfur dioxide, Nitrogen",
            _ => "Unknown"
        };
    }

    private static string HydrosphereFor(Planet planet)
    {
        return planet.TerrainStyle switch
        {
            PlanetTerrainStyle.Oceanic => "Water, Ice",
            PlanetTerrainStyle.Lush => "Water",
            PlanetTerrainStyle.Ice => "Ice, Water",
            PlanetTerrainStyle.Clouded => "Sulfuric acid, Water",
            PlanetTerrainStyle.Desert => "None",
            PlanetTerrainStyle.Rocky => "None",
            PlanetTerrainStyle.Volcanic => "Magma",
            PlanetTerrainStyle.GasBanded => "Metallic hydrogen",
            PlanetTerrainStyle.IceBanded => "Water ice, Ammonia",
            _ => "None"
        };
    }

    private static string LithosphereFor(Planet planet, IReadOnlyList<MineralTradeEntry>? minerals, Random rng)
    {
        if (planet.Composition == PlanetComposition.GasGiant)
        {
            return "None (gaseous)";
        }

        if (planet.Composition == PlanetComposition.IceGiant)
        {
            return "Ices, silicates";
        }

        var names = new List<string>();
        if (minerals != null)
        {
            for (int i = 0; i < minerals.Count; i++)
            {
                string name = minerals[i].Name;
                if (string.IsNullOrWhiteSpace(name) || string.Equals(name, "Endurium", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                names.Add(name);
            }
        }

        if (names.Count == 0)
        {
            names.AddRange(new[] { "Iron", "Nickel", "Silicon", "Aluminum", "Titanium" });
        }

        int count = Math.Clamp(3 + rng.Next(3), 3, Math.Min(5, names.Count));
        var picked = new List<string>(count);
        var remaining = new List<string>(names);
        for (int i = 0; i < count && remaining.Count > 0; i++)
        {
            int index = rng.Next(remaining.Count);
            picked.Add(remaining[index]);
            remaining.RemoveAt(index);
        }

        return string.Join(", ", picked);
    }

    private static int ClampPercent(int value) => Math.Clamp(value, 0, 100);

    private static List<SensorContact> BuildContacts(Planet planet, int bioPercent, int mineralPercent, Random rng)
    {
        var contacts = new List<SensorContact>();
        int lifeCount = bioPercent <= 0 ? 0 : Math.Clamp(bioPercent / 7, 1, 14);
        int mineralCount = mineralPercent <= 0 ? 0 : Math.Clamp(mineralPercent / 6, 1, 16);

        if (planet.TerrainStyle is PlanetTerrainStyle.GasBanded or PlanetTerrainStyle.IceBanded)
        {
            lifeCount = 0;
        }

        for (int i = 0; i < lifeCount; i++)
        {
            Vector2 p = RandomDiscPoint(rng, equatorialBias: 0.55f);
            contacts.Add(new SensorContact
            {
                X = p.X,
                Y = p.Y,
                Kind = SensorContactKind.Life,
                Color = LifeColor(rng)
            });
        }

        for (int i = 0; i < mineralCount; i++)
        {
            Vector2 p = RandomDiscPoint(rng, equatorialBias: 0.15f);
            contacts.Add(new SensorContact
            {
                X = p.X,
                Y = p.Y,
                Kind = SensorContactKind.Mineral,
                Color = MineralColor(rng)
            });
        }

        return contacts;
    }

    private static Vector2 RandomDiscPoint(Random rng, float equatorialBias)
    {
        for (int attempt = 0; attempt < 24; attempt++)
        {
            float x = (float)(rng.NextDouble() * 2.0 - 1.0);
            float y = (float)(rng.NextDouble() * 2.0 - 1.0);
            if (x * x + y * y > 0.88f)
            {
                continue;
            }

            if (equatorialBias > 0f && MathF.Abs(y) > 0.72f - equatorialBias * 0.4f && rng.NextDouble() < equatorialBias)
            {
                continue;
            }

            return new Vector2(x, y);
        }

        return new Vector2(0f, 0f);
    }

    private static Color LifeColor(Random rng)
    {
        return rng.Next(3) switch
        {
            0 => new Color(70, 230, 90, 255),
            1 => new Color(40, 190, 120, 255),
            _ => new Color(160, 255, 90, 255)
        };
    }

    private static Color MineralColor(Random rng)
    {
        return rng.Next(4) switch
        {
            0 => new Color(240, 200, 70, 255),
            1 => new Color(230, 140, 50, 255),
            2 => new Color(200, 200, 210, 255),
            _ => new Color(180, 80, 40, 255)
        };
    }
}
