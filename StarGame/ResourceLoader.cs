using System.Reflection;
using System.Text.Json;
using System.Numerics;
using Raylib_cs;

namespace StarflightGame;

public interface IResourceLoader
{
    List<StarSystem> LoadStarSystems();

    IReadOnlyDictionary<string, LoadedPlanet[]> LoadPlanetsByStarSystem();
}

public class ResourceLoader : IResourceLoader
{
    private const string StarSystemsResourceName = "StarflightGame.starSystems.json";

    private const string PlanetsResourceName = "StarflightGame.planets.json";

    private IReadOnlyDictionary<string, LoadedPlanet[]>? _cachedPlanetsByStarSystemId;

    public List<StarSystem> LoadStarSystems()
    {
        var assembly = Assembly.GetExecutingAssembly();

        using var stream = assembly.GetManifestResourceStream(StarSystemsResourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Could not find embedded resource: {StarSystemsResourceName}");
        }

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var starSystemData = JsonSerializer.Deserialize<List<StarSystemData>>(json, options);
        if (starSystemData == null)
        {
            throw new InvalidOperationException("Failed to deserialize star systems data");
        }

        var systems = new List<StarSystem>();
        foreach (var data in starSystemData)
        {
            var position = new Vector2(data.Position.X, data.Position.Y);
            var color = HexColor.ToRaylibColor(data.StarColor);
            systems.Add(new StarSystem(data.Id, data.Name, position, color));
        }

        return systems;
    }

    public IReadOnlyDictionary<string, LoadedPlanet[]> LoadPlanetsByStarSystem()
    {
        if (_cachedPlanetsByStarSystemId != null)
        {
            return _cachedPlanetsByStarSystemId;
        }

        var assembly = Assembly.GetExecutingAssembly();

        using var stream = assembly.GetManifestResourceStream(PlanetsResourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Could not find embedded resource: {PlanetsResourceName}");
        }

        using var reader = new StreamReader(stream);
        string json = reader.ReadToEnd();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var raw = JsonSerializer.Deserialize<Dictionary<string, List<InteriorPlanetDto>>>(json, options);
        if (raw == null || raw.Count == 0)
        {
            throw new InvalidOperationException("Failed to deserialize planets data");
        }

        var result = new Dictionary<string, LoadedPlanet[]>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, List<InteriorPlanetDto>> entry in raw)
        {
            string normalizedKey = entry.Key.ToLowerInvariant();
            List<InteriorPlanetDto> list = entry.Value;
            if (list == null || list.Count == 0)
            {
                result[normalizedKey] = Array.Empty<LoadedPlanet>();
                continue;
            }

            var converted = new LoadedPlanet[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                InteriorPlanetDto? d = list[i];
                if (d == null)
                {
                    throw new InvalidOperationException(
                        $"planets.json: null planet entry at index {i} for star system key '{entry.Key}'.");
                }

                if (string.IsNullOrWhiteSpace(d.SurfaceColor))
                {
                    throw new InvalidOperationException(
                        $"planets.json: missing surfaceColor for planet '{d.Name}' in star system '{entry.Key}'.");
                }

                PlanetComposition composition = ParseComposition(d.Composition, d.Name, entry.Key);

                float aAu = d.SemiMajorAxisAu > 0f ? d.SemiMajorAxisAu : 0.5f + i * 0.5f;
                float ecc = Math.Clamp(d.Eccentricity >= 0f ? d.Eccentricity : 0.05f, 0f, 0.95f);
                float omegaDeg = d.ArgumentOfPeriapsisDeg;
                PlanetRingData? rings = null;
                if (d.Rings != null)
                {
                    InteriorPlanetRingDto r = d.Rings;
                    Color ringColor = string.IsNullOrWhiteSpace(r.Color)
                        ? new Color(200, 190, 170, 255)
                        : HexColor.ToRaylibColor(r.Color);
                    rings = new PlanetRingData
                    {
                        InnerRadiusKm = r.InnerRadiusKm,
                        OuterRadiusKm = r.OuterRadiusKm,
                        ThicknessKm = r.ThicknessKm,
                        Opacity = Math.Clamp(r.Opacity, 0f, 1f),
                        RingColor = ringColor,
                        ParticleTexture = r.ParticleTexture ?? "",
                        HasGaps = r.HasGaps
                    };
                }

                converted[i] = new LoadedPlanet
                {
                    Name = d.Name,
                    IsFiction = d.IsFiction,
                    Composition = composition,
                    SurfaceColor = HexColor.ToRaylibColor(d.SurfaceColor),
                    SemiMajorAxisAu = aAu,
                    Eccentricity = ecc,
                    ArgumentOfPeriapsisRad = omegaDeg * (MathF.PI / 180f),
                    RadiusKm = d.RadiusKm > 0f ? d.RadiusKm : 0f,
                    Rings = rings
                };
            }

            result[normalizedKey] = converted;
        }

        _cachedPlanetsByStarSystemId = result;
        return result;
    }

    private static PlanetComposition ParseComposition(string? raw, string planetName, string starSystemKey)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return PlanetComposition.Terrestrial;
        }

        string key = raw.Trim().ToLowerInvariant();
        return key switch
        {
            "terrestrial" => PlanetComposition.Terrestrial,
            "gas_giant" => PlanetComposition.GasGiant,
            "ice_giant" => PlanetComposition.IceGiant,
            "molten" => PlanetComposition.Molten,
            _ => throw new InvalidOperationException(
                $"planets.json: invalid composition '{raw}' for planet '{planetName}' in star system '{starSystemKey}'. "
                + "Expected terrestrial, gas_giant, ice_giant, or molten.")
        };
    }

    private sealed class StarSystemData
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public Vector2Data Position { get; set; } = new();
        public string StarColor { get; set; } = "";
    }

    private sealed class Vector2Data
    {
        public float X { get; set; }
        public float Y { get; set; }
    }

    private sealed class InteriorPlanetDto
    {
        public string Name { get; set; } = "";
        public bool IsFiction { get; set; }
        public string Composition { get; set; } = "terrestrial";
        public string SurfaceColor { get; set; } = "";
        public float SemiMajorAxisAu { get; set; }
        public float Eccentricity { get; set; }
        public float ArgumentOfPeriapsisDeg { get; set; }
        public float RadiusKm { get; set; }

        public InteriorPlanetRingDto? Rings { get; set; }
    }

    private sealed class InteriorPlanetRingDto
    {
        public float InnerRadiusKm { get; set; }
        public float OuterRadiusKm { get; set; }
        public float ThicknessKm { get; set; }
        public float Opacity { get; set; }
        public string Color { get; set; } = "";
        public string ParticleTexture { get; set; } = "";
        public bool HasGaps { get; set; }
    }
}
