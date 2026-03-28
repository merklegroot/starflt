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
/// Tactical view of the local star system: central star, Keplerian ellipses (semi-major axis + eccentricity),
/// and flight with the ship at screen center.
/// </summary>
public sealed class StarSystemInteriorView : IStarSystemInteriorView
{
    private const int OrbitLineSegments = 96;

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
        if (n > 0)
        {
            float minAu = planets[0].SemiMajorAxisAu;
            float maxAu = planets[0].SemiMajorAxisAu;
            for (int j = 1; j < n; j++)
            {
                if (planets[j].SemiMajorAxisAu < minAu)
                {
                    minAu = planets[j].SemiMajorAxisAu;
                }

                if (planets[j].SemiMajorAxisAu > maxAu)
                {
                    maxAu = planets[j].SemiMajorAxisAu;
                }
            }

            Color dim = new Color(60, 65, 90, 120);

            for (int i = 0; i < n; i++)
            {
                LoadedPlanet p = planets[i];
                float aPx = MapSemiMajorAxisToPixels(p.SemiMajorAxisAu, minAu, maxAu, viewWidth, screenHeight);
                float e = Math.Clamp(p.Eccentricity, 0f, 0.95f);
                float omega = p.ArgumentOfPeriapsisRad;

                DrawEllipticalOrbit(starSx, starSy, aPx, e, omega, dim);

                float nu = i * (MathF.Tau / n) + 0.23f;
                EllipseRadiusAndWorldOffset(aPx, e, omega, nu, out float worldPx, out float worldPy);

                float psx = starSx + worldPx;
                float psy = starSy + worldPy;

                int pr = 6 + (i % 3);
                Color pc = p.SurfaceColor;
                Raylib.DrawCircle((int)psx, (int)psy, pr + 2, new Color(pc.R, pc.G, pc.B, (byte)100));
                Raylib.DrawCircle((int)psx, (int)psy, pr, pc);

                string label = p.Name;
                int fs = 16;
                int lw = Raylib.MeasureText(label, fs);
                Raylib.DrawText(label, (int)psx - lw / 2, (int)psy - pr - 20, fs, Color.LIGHTGRAY);
            }
        }

        DrawShipOnly(cx, cy, screenHeight, ship);
    }

    private static void DrawShipOnly(int cx, int cy, int screenHeight, IShip ship)
    {
        bool forwardThrust = ship.ManeuverThrustForward;
        bool reverseThrust = ship.ManeuverThrustReverse;
        ShipRenderer.Draw(cx, cy, ship.Rotation, forwardThrust, reverseThrust);

        Raylib.DrawText("STAR SYSTEM", 20, screenHeight - 56, 22, Color.SKYBLUE);
        Raylib.DrawText("A/D or arrows: turn | W/S: thrust / reverse | ESC: Canopy | X: Quit", 20, screenHeight - 28, 16, Color.YELLOW);
    }

    /// <summary>
    /// Maps semi-major axis (AU) to pixels for the tactical view; preserves relative scale within the system.
    /// </summary>
    private static float MapSemiMajorAxisToPixels(float semiMajorAxisAu, float minAu, float maxAu, int viewWidth, int screenHeight)
    {
        float maxPx = MathF.Min(viewWidth, screenHeight) * 0.36f;
        const float minPx = 48f;
        if (maxAu <= minAu + 1e-6f)
        {
            return (minPx + maxPx) * 0.5f;
        }

        float t = (semiMajorAxisAu - minAu) / (maxAu - minAu);
        return minPx + t * (maxPx - minPx);
    }

    /// <summary>
    /// Distance from the primary (star) at true anomaly ν for an ellipse with semi-major axis a and eccentricity e (polar form from focus).
    /// </summary>
    private static float RadiusFromTrueAnomaly(float semiMajorAxisPx, float eccentricity, float trueAnomalyRad)
    {
        float e = eccentricity;
        float cosNu = MathF.Cos(trueAnomalyRad);
        return semiMajorAxisPx * (1f - e * e) / (1f + e * cosNu);
    }

    private static void EllipseRadiusAndWorldOffset(
        float semiMajorAxisPx,
        float eccentricity,
        float argumentOfPeriapsisRad,
        float trueAnomalyRad,
        out float worldPx,
        out float worldPy)
    {
        float r = RadiusFromTrueAnomaly(semiMajorAxisPx, eccentricity, trueAnomalyRad);
        float angle = argumentOfPeriapsisRad + trueAnomalyRad;
        worldPx = r * MathF.Cos(angle);
        worldPy = r * MathF.Sin(angle);
    }

    private static void DrawEllipticalOrbit(
        float starScreenX,
        float starScreenY,
        float semiMajorAxisPx,
        float eccentricity,
        float argumentOfPeriapsisRad,
        Color color)
    {
        float e = eccentricity;
        float prevX = 0f;
        float prevY = 0f;
        bool hasPrev = false;

        for (int s = 0; s <= OrbitLineSegments; s++)
        {
            float nu = s * MathF.Tau / OrbitLineSegments;
            EllipseRadiusAndWorldOffset(semiMajorAxisPx, e, argumentOfPeriapsisRad, nu, out float wx, out float wy);
            float sx = starScreenX + wx;
            float sy = starScreenY + wy;

            if (hasPrev)
            {
                Raylib.DrawLine((int)prevX, (int)prevY, (int)sx, (int)sy, color);
            }

            prevX = sx;
            prevY = sy;
            hasPrev = true;
        }
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
                float aAu = d.SemiMajorAxisAu > 0f ? d.SemiMajorAxisAu : 0.5f + i * 0.5f;
                float ecc = Math.Clamp(d.Eccentricity >= 0f ? d.Eccentricity : 0.05f, 0f, 0.95f);
                float omegaDeg = d.ArgumentOfPeriapsisDeg;
                converted[i] = new LoadedPlanet
                {
                    Name = d.Name,
                    SurfaceColor = HexColor.ToRaylibColor(d.SurfaceColor),
                    SemiMajorAxisAu = aAu,
                    Eccentricity = ecc,
                    ArgumentOfPeriapsisRad = omegaDeg * (MathF.PI / 180f)
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
        public float SemiMajorAxisAu;
        public float Eccentricity;
        public float ArgumentOfPeriapsisRad;
    }

    private sealed class InteriorPlanetDto
    {
        public string Name { get; set; } = "";
        public required string SurfaceColor { get; set; }
        public float SemiMajorAxisAu { get; set; }
        public float Eccentricity { get; set; }
        public float ArgumentOfPeriapsisDeg { get; set; }
    }

    private static int Mod(int x, int m)
    {
        int r = x % m;
        return r < 0 ? r + m : r;
    }
}
