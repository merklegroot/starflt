using System.Numerics;
using Raylib_cs;

namespace StarflightGame;

public enum PlanetTerrainStyle
{
    Oceanic,
    Desert,
    Rocky,
    Ice,
    Lush,
    Clouded,
    GasBanded,
    IceBanded,
    Volcanic
}

/// <summary>
/// Names, composition, surface color, rings, and terrain style for catalog and random planets.
/// </summary>
public static class PlanetGenerator
{
    private static readonly string[] NamePrefixes =
    {
        "Al", "Bel", "Cor", "Dax", "Fen", "Gor", "Hel", "Ix", "Jor", "Kal",
        "Lyr", "Mor", "Nex", "Or", "Pal", "Quor", "Rho", "Thal", "Ul", "Vor",
        "Wyn", "Xan", "Yl", "Zor", "Ash", "Bry", "Cael", "Dre", "Esh", "Fir"
    };

    private static readonly string[] NameMiddles =
    {
        "a", "e", "i", "o", "u", "ar", "en", "il", "or", "un", "ae", "ia", "ou"
    };

    private static readonly string[] NameSuffixes =
    {
        "a", "ia", "os", "on", "is", "e", "us", "or", "an", "ith", "ara", "um", "ix", "or"
    };

    private static readonly string[] RomanNumerals =
    {
        "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII"
    };

    public static Planet CreateRandom(string systemName)
    {
        Random rng = new Random();
        PlanetComposition composition = PickComposition(rng);
        PlanetTerrainStyle style = PickStyle(composition, rng);
        Color surfaceColor = PickSurfaceColor(style, rng);
        float radiusKm = PickRadiusKm(composition, rng);
        PlanetRingData? rings = MaybeCreateRings(composition, surfaceColor, radiusKm, rng);
        string name = CreateName(systemName, rng);

        return new Planet(name, Vector2.Zero, 50f, surfaceColor, radiusKm, rings, composition, style);
    }

    public static string CreateName(string systemName)
    {
        return CreateName(systemName, new Random());
    }

    public static string CreateName(string systemName, Random rng)
    {
        if (!string.IsNullOrWhiteSpace(systemName) && rng.NextDouble() < 0.28)
        {
            return $"{systemName.Trim()} {RomanNumerals[rng.Next(RomanNumerals.Length)]}";
        }

        string name = NamePrefixes[rng.Next(NamePrefixes.Length)]
            + NameMiddles[rng.Next(NameMiddles.Length)]
            + NameSuffixes[rng.Next(NameSuffixes.Length)];

        if (rng.NextDouble() < 0.35)
        {
            name += " " + RomanNumerals[rng.Next(RomanNumerals.Length)];
        }

        return name;
    }

    public static PlanetTerrainStyle ClassifyStyle(PlanetComposition composition, Color surfaceColor)
    {
        if (composition == PlanetComposition.GasGiant)
        {
            return PlanetTerrainStyle.GasBanded;
        }

        if (composition == PlanetComposition.IceGiant)
        {
            return PlanetTerrainStyle.IceBanded;
        }

        if (composition == PlanetComposition.Molten)
        {
            return PlanetTerrainStyle.Volcanic;
        }

        return ClassifyTerrestrialStyle(surfaceColor);
    }

    public static string FormatTerrainLabel(PlanetTerrainStyle style)
    {
        return style switch
        {
            PlanetTerrainStyle.Oceanic => "Oceans and continents",
            PlanetTerrainStyle.Desert => "Arid",
            PlanetTerrainStyle.Rocky => "Airless rock",
            PlanetTerrainStyle.Ice => "Icy",
            PlanetTerrainStyle.Lush => "Vegetated",
            PlanetTerrainStyle.Clouded => "Cloud-covered",
            PlanetTerrainStyle.GasBanded => "Banded atmosphere",
            PlanetTerrainStyle.IceBanded => "Methane haze",
            PlanetTerrainStyle.Volcanic => "Volcanic",
            _ => style.ToString()
        };
    }

    public static int StableHash(string value)
    {
        unchecked
        {
            int hash = (int)2166136261;
            foreach (char c in value)
            {
                hash = (hash ^ c) * 16777619;
            }

            return hash;
        }
    }

    private static PlanetComposition PickComposition(Random rng)
    {
        int roll = rng.Next(100);
        if (roll < 48)
        {
            return PlanetComposition.Terrestrial;
        }

        if (roll < 64)
        {
            return PlanetComposition.Molten;
        }

        if (roll < 80)
        {
            return PlanetComposition.IceGiant;
        }

        return PlanetComposition.GasGiant;
    }

    private static PlanetTerrainStyle PickStyle(PlanetComposition composition, Random rng)
    {
        if (composition != PlanetComposition.Terrestrial)
        {
            return ClassifyStyle(composition, Color.GRAY);
        }

        return rng.Next(6) switch
        {
            0 => PlanetTerrainStyle.Oceanic,
            1 => PlanetTerrainStyle.Desert,
            2 => PlanetTerrainStyle.Rocky,
            3 => PlanetTerrainStyle.Ice,
            4 => PlanetTerrainStyle.Lush,
            _ => PlanetTerrainStyle.Clouded
        };
    }

    private static Color PickSurfaceColor(PlanetTerrainStyle style, Random rng)
    {
        return style switch
        {
            PlanetTerrainStyle.Oceanic => Jitter(new Color(80, 130, 200, 255), 28, rng),
            PlanetTerrainStyle.Desert => Jitter(new Color(190, 90, 45, 255), 32, rng),
            PlanetTerrainStyle.Rocky => Jitter(new Color(150, 148, 142, 255), 24, rng),
            PlanetTerrainStyle.Ice => Jitter(new Color(210, 220, 230, 255), 18, rng),
            PlanetTerrainStyle.Lush => Jitter(new Color(70, 130, 80, 255), 26, rng),
            PlanetTerrainStyle.Clouded => Jitter(new Color(220, 190, 120, 255), 22, rng),
            PlanetTerrainStyle.GasBanded => Jitter(PickGasBase(rng), 20, rng),
            PlanetTerrainStyle.IceBanded => Jitter(new Color(80, 160, 210, 255), 36, rng),
            PlanetTerrainStyle.Volcanic => Jitter(new Color(90, 40, 28, 255), 22, rng),
            _ => new Color(100, 120, 90, 255)
        };
    }

    private static Color PickGasBase(Random rng)
    {
        return rng.Next(4) switch
        {
            0 => new Color(212, 165, 116, 255),
            1 => new Color(232, 212, 168, 255),
            2 => new Color(180, 90, 70, 255),
            _ => new Color(122, 184, 212, 255)
        };
    }

    private static float PickRadiusKm(PlanetComposition composition, Random rng)
    {
        return composition switch
        {
            PlanetComposition.GasGiant => 38000f + rng.NextSingle() * 40000f,
            PlanetComposition.IceGiant => 16000f + rng.NextSingle() * 14000f,
            PlanetComposition.Molten => 2800f + rng.NextSingle() * 4500f,
            _ => 1800f + rng.NextSingle() * 6200f
        };
    }

    private static PlanetRingData? MaybeCreateRings(
        PlanetComposition composition,
        Color surfaceColor,
        float radiusKm,
        Random rng)
    {
        float chance = composition switch
        {
            PlanetComposition.GasGiant => 0.55f,
            PlanetComposition.IceGiant => 0.28f,
            _ => 0.06f
        };

        if (rng.NextDouble() >= chance)
        {
            return null;
        }

        float inner = radiusKm * (1.12f + rng.NextSingle() * 0.45f);
        float outer = inner + radiusKm * (0.35f + rng.NextSingle() * 1.6f);
        Color ringColor = LerpColor(surfaceColor, new Color(230, 220, 200, 255), 0.45f);

        return new PlanetRingData
        {
            InnerRadiusKm = inner,
            OuterRadiusKm = outer,
            ThicknessKm = 0.1f + rng.NextSingle() * 0.4f,
            Opacity = 0.35f + rng.NextSingle() * 0.55f,
            RingColor = ringColor,
            ParticleTexture = rng.NextDouble() < 0.5 ? "bright_ice" : "dusty",
            HasGaps = rng.NextDouble() < 0.55
        };
    }

    private static PlanetTerrainStyle ClassifyTerrestrialStyle(Color surfaceColor)
    {
        float r = surfaceColor.R;
        float g = surfaceColor.G;
        float b = surfaceColor.B;
        float max = MathF.Max(r, MathF.Max(g, b));
        float min = MathF.Min(r, MathF.Min(g, b));
        float sat = max <= 1f ? 0f : (max - min) / max;
        float lum = (r + g + b) / (3f * 255f);

        if (sat < 0.16f)
        {
            return lum > 0.78f ? PlanetTerrainStyle.Ice : PlanetTerrainStyle.Rocky;
        }

        if (b > r + 12f && b >= g)
        {
            return PlanetTerrainStyle.Oceanic;
        }

        if (g > r && g > b && g > 90f)
        {
            return PlanetTerrainStyle.Lush;
        }

        if (r > 155f && g > 125f && b < 150f)
        {
            return PlanetTerrainStyle.Clouded;
        }

        if (r > g && r > b && g >= b * 0.7f)
        {
            return PlanetTerrainStyle.Desert;
        }

        if (lum > 0.72f && b >= g)
        {
            return PlanetTerrainStyle.Ice;
        }

        return PlanetTerrainStyle.Rocky;
    }

    private static Color Jitter(Color color, int amount, Random rng)
    {
        return new Color(
            ClampByte(color.R + rng.Next(-amount, amount + 1)),
            ClampByte(color.G + rng.Next(-amount, amount + 1)),
            ClampByte(color.B + rng.Next(-amount, amount + 1)),
            (byte)255);
    }

    private static Color LerpColor(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new Color(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t),
            (byte)255);
    }

    private static byte ClampByte(int value) => (byte)Math.Clamp(value, 0, 255);
}
