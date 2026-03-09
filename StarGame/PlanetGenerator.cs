using Raylib_cs;
using System.Numerics;
using System;

namespace StarflightGame;

public enum PlanetType
{
    Terran,      // Earth-like, habitable
    Ocean,       // Water world
    Desert,      // Arid, sandy
    Ice,         // Frozen world
    Volcanic,    // Molten surface
    GasGiant,    // Large gas planet
    Barren,      // Rocky, no atmosphere
    Toxic,       // Poisonous atmosphere
    Jungle,      // Dense vegetation
    Tundra,      // Cold but not frozen
    Arid,        // Dry, minimal water
    Crystalline  // Crystal formations
}

public class PlanetCharacteristics
{
    public PlanetType Type { get; set; }
    public Color BaseColor { get; set; }
    public Color AccentColor { get; set; }
    public string NamePrefix { get; set; } = "";
    public string NameSuffix { get; set; } = "";
    public float RadiusMultiplier { get; set; } = 1.0f;
    public int MineralRichness { get; set; } = 5; // 1-10 scale
    public bool HasAtmosphere { get; set; } = true;
    public string Description { get; set; } = "";
}

public static class PlanetGenerator
{
    private static readonly string[] TerranPrefixes = { "Terra", "Gaia", "Eden", "New", "Prime", "Alpha" };
    private static readonly string[] OceanPrefixes = { "Aqua", "Neptune", "Poseidon", "Marine", "Ocean", "Hydro" };
    private static readonly string[] DesertPrefixes = { "Arid", "Dune", "Sahara", "Desert", "Sand", "Dust" };
    private static readonly string[] IcePrefixes = { "Frost", "Ice", "Glacier", "Frozen", "Cryo", "Polar" };
    private static readonly string[] VolcanicPrefixes = { "Vulcan", "Magma", "Inferno", "Lava", "Fire", "Ember" };
    private static readonly string[] GasGiantPrefixes = { "Jupiter", "Saturn", "Gas", "Storm", "Cloud", "Nebula" };
    private static readonly string[] BarrenPrefixes = { "Rock", "Stone", "Asteroid", "Barren", "Void", "Dead" };
    private static readonly string[] ToxicPrefixes = { "Venom", "Poison", "Toxic", "Acid", "Corrosive", "Fume" };
    private static readonly string[] JunglePrefixes = { "Jungle", "Forest", "Verdant", "Green", "Flora", "Bio" };
    private static readonly string[] TundraPrefixes = { "Tundra", "Frost", "Cold", "Arctic", "Boreal", "Chill" };
    private static readonly string[] AridPrefixes = { "Arid", "Dry", "Parched", "Scorch", "Bleak", "Waste" };
    private static readonly string[] CrystallinePrefixes = { "Crystal", "Gem", "Prism", "Shard", "Diamond", "Quartz" };

    private static readonly string[] Suffixes = { "I", "II", "III", "IV", "V", "VI", "VII", "Alpha", "Beta", "Gamma", "Prime", "Secundus" };

    public static Planet GeneratePlanet(string systemName, Vector2 systemPosition, int orbitIndex, Random random)
    {
        // Generate planet characteristics
        var characteristics = GenerateCharacteristics(random);
        
        // Generate name
        string planetName = GeneratePlanetName(systemName, orbitIndex, characteristics, random);
        
        // Calculate orbital position
        float angle = (float)(random.NextDouble() * Math.PI * 2);
        float baseDistance = 50 + orbitIndex * 30;
        float distanceVariation = (float)(random.NextDouble() * 20 - 10); // ±10 units
        float distance = baseDistance + distanceVariation;
        
        Vector2 planetPos = systemPosition + new Vector2(
            (float)Math.Cos(angle) * distance,
            (float)Math.Sin(angle) * distance
        );
        
        // Generate radius based on type
        float baseRadius = 35.0f;
        float radius = baseRadius * characteristics.RadiusMultiplier + (float)(random.NextDouble() * 15 - 5);
        radius = Math.Max(25.0f, Math.Min(60.0f, radius)); // Clamp between 25-60
        
        // Create planet with generated color
        Planet planet = new Planet(planetName, planetPos, radius, characteristics.BaseColor);
        
        // Store additional characteristics if needed (could extend Planet class)
        // For now, the color and name convey the type
        
        return planet;
    }

    private static PlanetCharacteristics GenerateCharacteristics(Random random)
    {
        var characteristics = new PlanetCharacteristics();
        
        // Weighted random selection for planet types
        // More common types appear more frequently
        float typeRoll = (float)random.NextDouble();
        
        if (typeRoll < 0.15f)
            characteristics.Type = PlanetType.Terran;
        else if (typeRoll < 0.25f)
            characteristics.Type = PlanetType.Ocean;
        else if (typeRoll < 0.35f)
            characteristics.Type = PlanetType.Desert;
        else if (typeRoll < 0.45f)
            characteristics.Type = PlanetType.Barren;
        else if (typeRoll < 0.55f)
            characteristics.Type = PlanetType.Ice;
        else if (typeRoll < 0.62f)
            characteristics.Type = PlanetType.Arid;
        else if (typeRoll < 0.69f)
            characteristics.Type = PlanetType.Tundra;
        else if (typeRoll < 0.75f)
            characteristics.Type = PlanetType.Volcanic;
        else if (typeRoll < 0.80f)
            characteristics.Type = PlanetType.GasGiant;
        else if (typeRoll < 0.85f)
            characteristics.Type = PlanetType.Jungle;
        else if (typeRoll < 0.90f)
            characteristics.Type = PlanetType.Toxic;
        else
            characteristics.Type = PlanetType.Crystalline;
        
        // Generate colors and properties based on type
        switch (characteristics.Type)
        {
            case PlanetType.Terran:
                characteristics.BaseColor = GenerateTerranColor(random);
                characteristics.AccentColor = new Color((byte)100, (byte)150, (byte)200, (byte)255); // Blue oceans
                characteristics.RadiusMultiplier = 0.9f + (float)(random.NextDouble() * 0.2f);
                characteristics.MineralRichness = random.Next(4, 8);
                characteristics.HasAtmosphere = true;
                characteristics.Description = "Earth-like world with oceans and continents";
                break;
                
            case PlanetType.Ocean:
                characteristics.BaseColor = GenerateOceanColor(random);
                characteristics.AccentColor = new Color((byte)50, (byte)100, (byte)150, (byte)255);
                characteristics.RadiusMultiplier = 1.0f + (float)(random.NextDouble() * 0.3f);
                characteristics.MineralRichness = random.Next(3, 7);
                characteristics.HasAtmosphere = true;
                characteristics.Description = "Water world with vast oceans";
                break;
                
            case PlanetType.Desert:
                characteristics.BaseColor = GenerateDesertColor(random);
                characteristics.AccentColor = new Color((byte)200, (byte)180, (byte)150, (byte)255);
                characteristics.RadiusMultiplier = 0.8f + (float)(random.NextDouble() * 0.2f);
                characteristics.MineralRichness = random.Next(5, 9);
                characteristics.HasAtmosphere = random.NextDouble() > 0.3f;
                characteristics.Description = "Arid desert world";
                break;
                
            case PlanetType.Ice:
                characteristics.BaseColor = GenerateIceColor(random);
                characteristics.AccentColor = new Color((byte)200, (byte)220, (byte)255, (byte)255);
                characteristics.RadiusMultiplier = 0.85f + (float)(random.NextDouble() * 0.25f);
                characteristics.MineralRichness = random.Next(2, 6);
                characteristics.HasAtmosphere = random.NextDouble() > 0.4f;
                characteristics.Description = "Frozen ice world";
                break;
                
            case PlanetType.Volcanic:
                characteristics.BaseColor = GenerateVolcanicColor(random);
                characteristics.AccentColor = new Color((byte)255, (byte)100, (byte)50, (byte)255);
                characteristics.RadiusMultiplier = 0.9f + (float)(random.NextDouble() * 0.2f);
                characteristics.MineralRichness = random.Next(6, 10);
                characteristics.HasAtmosphere = true;
                characteristics.Description = "Volcanic world with active geology";
                break;
                
            case PlanetType.GasGiant:
                characteristics.BaseColor = GenerateGasGiantColor(random);
                characteristics.AccentColor = new Color((byte)150, (byte)150, (byte)200, (byte)255);
                characteristics.RadiusMultiplier = 1.5f + (float)(random.NextDouble() * 0.5f);
                characteristics.MineralRichness = random.Next(1, 4);
                characteristics.HasAtmosphere = true;
                characteristics.Description = "Massive gas giant";
                break;
                
            case PlanetType.Barren:
                characteristics.BaseColor = GenerateBarrenColor(random);
                characteristics.AccentColor = new Color((byte)100, (byte)100, (byte)100, (byte)255);
                characteristics.RadiusMultiplier = 0.7f + (float)(random.NextDouble() * 0.2f);
                characteristics.MineralRichness = random.Next(4, 8);
                characteristics.HasAtmosphere = false;
                characteristics.Description = "Barren rocky world";
                break;
                
            case PlanetType.Toxic:
                characteristics.BaseColor = GenerateToxicColor(random);
                characteristics.AccentColor = new Color((byte)150, (byte)255, (byte)100, (byte)255);
                characteristics.RadiusMultiplier = 0.85f + (float)(random.NextDouble() * 0.25f);
                characteristics.MineralRichness = random.Next(3, 7);
                characteristics.HasAtmosphere = true;
                characteristics.Description = "Toxic world with poisonous atmosphere";
                break;
                
            case PlanetType.Jungle:
                characteristics.BaseColor = GenerateJungleColor(random);
                characteristics.AccentColor = new Color((byte)50, (byte)150, (byte)50, (byte)255);
                characteristics.RadiusMultiplier = 0.95f + (float)(random.NextDouble() * 0.2f);
                characteristics.MineralRichness = random.Next(4, 8);
                characteristics.HasAtmosphere = true;
                characteristics.Description = "Dense jungle world";
                break;
                
            case PlanetType.Tundra:
                characteristics.BaseColor = GenerateTundraColor(random);
                characteristics.AccentColor = new Color((byte)180, (byte)200, (byte)220, (byte)255);
                characteristics.RadiusMultiplier = 0.9f + (float)(random.NextDouble() * 0.2f);
                characteristics.MineralRichness = random.Next(3, 7);
                characteristics.HasAtmosphere = true;
                characteristics.Description = "Cold tundra world";
                break;
                
            case PlanetType.Arid:
                characteristics.BaseColor = GenerateAridColor(random);
                characteristics.AccentColor = new Color((byte)180, (byte)160, (byte)140, (byte)255);
                characteristics.RadiusMultiplier = 0.8f + (float)(random.NextDouble() * 0.2f);
                characteristics.MineralRichness = random.Next(4, 8);
                characteristics.HasAtmosphere = random.NextDouble() > 0.5f;
                characteristics.Description = "Dry arid world";
                break;
                
            case PlanetType.Crystalline:
                characteristics.BaseColor = GenerateCrystallineColor(random);
                characteristics.AccentColor = new Color((byte)200, (byte)200, (byte)255, (byte)255);
                characteristics.RadiusMultiplier = 0.75f + (float)(random.NextDouble() * 0.25f);
                characteristics.MineralRichness = random.Next(7, 10);
                characteristics.HasAtmosphere = random.NextDouble() > 0.6f;
                characteristics.Description = "Crystalline world with gem formations";
                break;
        }
        
        return characteristics;
    }

    private static string GeneratePlanetName(string systemName, int orbitIndex, PlanetCharacteristics characteristics, Random random)
    {
        string prefix = "";
        
        switch (characteristics.Type)
        {
            case PlanetType.Terran:
                prefix = TerranPrefixes[random.Next(TerranPrefixes.Length)];
                break;
            case PlanetType.Ocean:
                prefix = OceanPrefixes[random.Next(OceanPrefixes.Length)];
                break;
            case PlanetType.Desert:
                prefix = DesertPrefixes[random.Next(DesertPrefixes.Length)];
                break;
            case PlanetType.Ice:
                prefix = IcePrefixes[random.Next(IcePrefixes.Length)];
                break;
            case PlanetType.Volcanic:
                prefix = VolcanicPrefixes[random.Next(VolcanicPrefixes.Length)];
                break;
            case PlanetType.GasGiant:
                prefix = GasGiantPrefixes[random.Next(GasGiantPrefixes.Length)];
                break;
            case PlanetType.Barren:
                prefix = BarrenPrefixes[random.Next(BarrenPrefixes.Length)];
                break;
            case PlanetType.Toxic:
                prefix = ToxicPrefixes[random.Next(ToxicPrefixes.Length)];
                break;
            case PlanetType.Jungle:
                prefix = JunglePrefixes[random.Next(JunglePrefixes.Length)];
                break;
            case PlanetType.Tundra:
                prefix = TundraPrefixes[random.Next(TundraPrefixes.Length)];
                break;
            case PlanetType.Arid:
                prefix = AridPrefixes[random.Next(AridPrefixes.Length)];
                break;
            case PlanetType.Crystalline:
                prefix = CrystallinePrefixes[random.Next(CrystallinePrefixes.Length)];
                break;
        }
        
        // Sometimes use system name, sometimes just the prefix
        if (random.NextDouble() < 0.4f)
        {
            return $"{systemName} {prefix}";
        }
        else
        {
            string suffix = Suffixes[random.Next(Suffixes.Length)];
            return $"{prefix} {suffix}";
        }
    }

    // Color generation methods for each planet type
    private static Color GenerateTerranColor(Random random)
    {
        // Earth-like: blues, greens, browns
        int r = random.Next(50, 100);
        int g = random.Next(100, 150);
        int b = random.Next(80, 120);
        return new Color((byte)r, (byte)g, (byte)b, (byte)255);
    }

    private static Color GenerateOceanColor(Random random)
    {
        // Ocean: various shades of blue
        int baseBlue = random.Next(30, 80);
        int g = baseBlue + random.Next(20, 40);
        int bValue = baseBlue + random.Next(40, 60);
        return new Color((byte)baseBlue, (byte)g, (byte)bValue, (byte)255);
    }

    private static Color GenerateDesertColor(Random random)
    {
        // Desert: tans, oranges, yellows
        int r = random.Next(150, 200);
        int g = random.Next(120, 170);
        int b = random.Next(80, 120);
        return new Color((byte)r, (byte)g, (byte)b, (byte)255);
    }

    private static Color GenerateIceColor(Random random)
    {
        // Ice: light blues, grays, whites
        int baseValue = random.Next(180, 240);
        int variation = random.Next(-20, 20);
        return new Color((byte)baseValue, (byte)(baseValue + variation), (byte)255, (byte)255);
    }

    private static Color GenerateVolcanicColor(Random random)
    {
        // Volcanic: reds, oranges, dark grays
        int r = random.Next(100, 180);
        int g = random.Next(50, 100);
        int b = random.Next(30, 60);
        return new Color((byte)r, (byte)g, (byte)b, (byte)255);
    }

    private static Color GenerateGasGiantColor(Random random)
    {
        // Gas giant: various colors (Jupiter-like)
        float colorChoice = (float)random.NextDouble();
        if (colorChoice < 0.4f)
        {
            // Jupiter-like: oranges and browns
            return new Color((byte)random.Next(180, 220), (byte)random.Next(140, 180), (byte)random.Next(100, 140), (byte)255);
        }
        else if (colorChoice < 0.7f)
        {
            // Blue gas giant
            return new Color((byte)random.Next(80, 120), (byte)random.Next(120, 160), (byte)random.Next(180, 220), (byte)255);
        }
        else
        {
            // Gray/white gas giant
            int gray = random.Next(150, 200);
            return new Color((byte)gray, (byte)gray, (byte)gray, (byte)255);
        }
    }

    private static Color GenerateBarrenColor(Random random)
    {
        // Barren: grays, browns
        int gray = random.Next(80, 140);
        int variation = random.Next(-20, 20);
        return new Color((byte)(gray + variation), (byte)gray, (byte)(gray - variation), (byte)255);
    }

    private static Color GenerateToxicColor(Random random)
    {
        // Toxic: greens, yellows, sickly colors
        int r = random.Next(100, 150);
        int g = random.Next(150, 200);
        int b = random.Next(80, 120);
        return new Color((byte)r, (byte)g, (byte)b, (byte)255);
    }

    private static Color GenerateJungleColor(Random random)
    {
        // Jungle: deep greens
        int r = random.Next(30, 60);
        int g = random.Next(100, 150);
        int b = random.Next(40, 80);
        return new Color((byte)r, (byte)g, (byte)b, (byte)255);
    }

    private static Color GenerateTundraColor(Random random)
    {
        // Tundra: cool grays and light blues
        int baseValue = random.Next(140, 180);
        int b = baseValue + random.Next(20, 40);
        return new Color((byte)baseValue, (byte)baseValue, (byte)b, (byte)255);
    }

    private static Color GenerateAridColor(Random random)
    {
        // Arid: browns, tans
        int r = random.Next(120, 160);
        int g = random.Next(100, 140);
        int b = random.Next(80, 120);
        return new Color((byte)r, (byte)g, (byte)b, (byte)255);
    }

    private static Color GenerateCrystallineColor(Random random)
    {
        // Crystalline: bright, saturated colors
        float colorChoice = (float)random.NextDouble();
        if (colorChoice < 0.33f)
        {
            // Blue crystals
            return new Color((byte)random.Next(100, 150), (byte)random.Next(150, 200), (byte)255, (byte)255);
        }
        else if (colorChoice < 0.66f)
        {
            // Purple crystals
            return new Color((byte)random.Next(150, 200), (byte)random.Next(100, 150), (byte)255, (byte)255);
        }
        else
        {
            // Pink/red crystals
            return new Color((byte)255, (byte)random.Next(150, 200), (byte)random.Next(200, 255), (byte)255);
        }
    }
}
