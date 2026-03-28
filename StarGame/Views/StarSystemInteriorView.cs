using Raylib_cs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Text.Json;

namespace StarflightGame.Views;

public interface IStarSystemInteriorView
{
    void Draw(StarSystem? system, int viewWidth, int screenHeight, Vector2 shipSystemPosition, IShip ship);
}


/// <summary>
/// Tactical view of the local star system: central star, fixed orbits, and flight with the ship at screen center.
/// </summary>
public sealed class StarSystemInteriorView : IStarSystemInteriorView
{
    private static readonly Dictionary<string, LoadedPlanet[]> _planetsByStarSystemId = LoadPlanetsByStarSystem();

    public void Draw(StarSystem? system, int viewWidth, int screenHeight, Vector2 shipSystemPosition, IShip ship)
    {
        int cx = viewWidth / 2;
        int cy = screenHeight / 2;
        float spx = shipSystemPosition.X;
        float spy = shipSystemPosition.Y;

        Raylib.DrawRectangle(0, 0, viewWidth, screenHeight, new Color(8, 10, 22, 255));

        for (int i = 0; i < 80; i++)
        {
            int sx = Mod(i * 97 + (int)spx, viewWidth);
            int sy = Mod(i * 53 + (int)spy, screenHeight);
            Raylib.DrawPixel(sx, sy, new Color(40, 45, 70, 255));
        }

        float starSx = cx - spx;
        float starSy = cy - spy;

        Color starColor = system?.StarColor ?? new Color(255, 220, 160, 255);
        const int starRadius = 36;

        Raylib.DrawCircle((int)starSx, (int)starSy, starRadius + 28, new Color(starColor.R, starColor.G, starColor.B, (byte)25));
        Raylib.DrawCircle((int)starSx, (int)starSy, starRadius + 14, new Color(starColor.R, starColor.G, starColor.B, (byte)60));
        Raylib.DrawCircle((int)starSx, (int)starSy, starRadius, starColor);
        Raylib.DrawCircle((int)starSx, (int)starSy, starRadius / 2, new Color((byte)255, (byte)255, (byte)255, (byte)200));

        string systemTitle = system != null ? system.Name : "Unknown system";
        int titleW = Raylib.MeasureText(systemTitle, 28);
        Raylib.DrawText(systemTitle, cx - titleW / 2, 24, 28, Color.WHITE);

        LoadedPlanet[] planets = ResolvePlanets(system);

        int n = planets.Length;
        for (int i = 0; i < n; i++)
        {
            float orbitR = 70f + i * 38f;
            float angle = i * (MathF.Tau / n);
            float worldPx = MathF.Cos(angle) * orbitR;
            float worldPy = MathF.Sin(angle) * orbitR;

            float psx = cx + worldPx - spx;
            float psy = cy + worldPy - spy;

            Color dim = new Color(60, 65, 90, 120);
            Raylib.DrawCircleLines((int)starSx, (int)starSy, (int)orbitR, dim);

            int pr = 6 + (i % 3);
            Color pc = planets[i].SurfaceColor;
            Raylib.DrawCircle((int)psx, (int)psy, pr + 2, new Color(pc.R, pc.G, pc.B, (byte)100));
            Raylib.DrawCircle((int)psx, (int)psy, pr, pc);

            string label = planets[i].Name;
            int fs = 16;
            int lw = Raylib.MeasureText(label, fs);
            Raylib.DrawText(label, (int)psx - lw / 2, (int)psy - pr - 20, fs, Color.LIGHTGRAY);
        }

        bool forwardThrust = ship.ManeuverThrustForward;
        bool reverseThrust = ship.ManeuverThrustReverse;
        ShipRenderer.Draw(cx, cy, ship.Rotation, forwardThrust, reverseThrust);

        Raylib.DrawText("STAR SYSTEM", 20, screenHeight - 56, 22, Color.SKYBLUE);
        Raylib.DrawText("A/D or arrows: turn | W/S: thrust / reverse | ESC: Canopy | X: Quit", 20, screenHeight - 28, 16, Color.YELLOW);
    }

    private static LoadedPlanet[] ResolvePlanets(StarSystem? system)
    {
        if (system == null || string.IsNullOrEmpty(system.Id))
        {
            return Array.Empty<LoadedPlanet>();
        }

        if (_planetsByStarSystemId.TryGetValue(system.Id, out LoadedPlanet[]? planets))
        {
            return planets;
        }

        return Array.Empty<LoadedPlanet>();
    }

    private static Dictionary<string, LoadedPlanet[]> LoadPlanetsByStarSystem()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "StarflightGame.planets.json";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
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
            List<InteriorPlanetDto> list = entry.Value;
            if (list == null || list.Count == 0)
            {
                result[entry.Key] = Array.Empty<LoadedPlanet>();
                continue;
            }

            var converted = new LoadedPlanet[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                InteriorPlanetDto d = list[i];
                converted[i] = new LoadedPlanet
                {
                    Name = d.Name,
                    SurfaceColor = HexColor.ToRaylibColor(d.SurfaceColor)
                };
            }

            result[entry.Key] = converted;
        }

        return result;
    }

    private struct LoadedPlanet
    {
        public string Name;
        public Color SurfaceColor;
    }

    private sealed class InteriorPlanetDto
    {
        public string Name { get; set; } = "";
        public required string SurfaceColor { get; set; }
    }

    private static int Mod(int x, int m)
    {
        int r = x % m;
        return r < 0 ? r + m : r;
    }
}
