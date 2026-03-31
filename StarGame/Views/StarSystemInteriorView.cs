using Raylib_cs;
using StarflightGame;
using System.Numerics;

namespace StarflightGame.Views;

public interface IStarSystemInteriorView
{
    /// <summary>
    /// Call when the player enters star system interior view so each planet can pick a new random orbital position.
    /// </summary>
    void NotifyStarSystemViewEntered(StarSystem? system);

    /// <summary>
    /// Input for overlays drawn in the main star system view (e.g. planet list toggle).
    /// </summary>
    void UpdateStarSystemUiInput();

    void Draw(StarSystem? system, int viewWidth, int screenHeight, Vector2 shipSystemPosition, IShip ship);

    /// <summary>
    /// Star-centered overview of orbits and bodies for the right panel (call after the main star system draw in the same frame).
    /// </summary>
    void DrawOverviewMap(
        int panelX,
        int topY,
        int width,
        int height,
        StarSystem? system,
        Vector2 shipWorldPosition,
        int mainViewWidth,
        int mainViewHeight);
}


/// <summary>
/// Tactical view of the local star system: central star, Keplerian ellipses (semi-major axis + eccentricity),
/// and flight with the ship at screen center.
/// </summary>
public sealed class StarSystemInteriorView : IStarSystemInteriorView
{
    /// <summary>World-to-screen scale for the tactical view (larger = more zoomed in).</summary>
    private const float InteriorViewZoom = 4f;

    private const int OrbitLineSegments = 96;

    /// <summary>
    /// Real-time seconds that correspond to one Earth orbital period at 1 AU (Kepler: T ∝ a^(3/2) years).
    /// Smaller = faster apparent motion.
    /// </summary>
    private const float SimulatedSecondsPerEarthYear = 250f;

    private readonly IReadOnlyDictionary<string, LoadedPlanet[]> _planetsByStarSystemId;

    private string? _orbitPhasesSystemId;
    private float[] _meanAnomalyAtEpochRad = Array.Empty<float>();
    private float[] _meanMotionRadPerSec = Array.Empty<float>();
    private float _orbitElapsedSeconds;
    private bool _planetListVisible = true;

    public StarSystemInteriorView(IResourceLoader resourceLoader)
    {
        _planetsByStarSystemId = resourceLoader.LoadPlanetsByStarSystem();
    }

    public void UpdateStarSystemUiInput()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_P))
        {
            _planetListVisible = !_planetListVisible;
        }
    }

    public void NotifyStarSystemViewEntered(StarSystem? system)
    {
        ResampleOrbitPhases(system);
    }

    public void Draw(StarSystem? system, int viewWidth, int screenHeight, Vector2 shipSystemPosition, IShip ship)
    {
        int cx = viewWidth / 2;
        int cy = screenHeight / 2;
        float spx = shipSystemPosition.X;
        float spy = shipSystemPosition.Y;
        float z = InteriorViewZoom;

        Raylib.DrawRectangle(0, 0, viewWidth, screenHeight, new Color(8, 10, 22, 255));

        for (int i = 0; i < 80; i++)
        {
            int sx = Mod(i * 97 + (int)spx, viewWidth);
            int sy = Mod(i * 53 + (int)spy, screenHeight);
            Raylib.DrawPixel(sx, sy, new Color(40, 45, 70, 255));
        }

        float starSx = cx - spx * z;
        float starSy = cy - spy * z;

        Color starColor = system?.StarColor ?? new Color(255, 220, 160, 255);
        float starRadius = 36f * z;

        Raylib.DrawCircle((int)starSx, (int)starSy, starRadius + 28f * z, new Color(starColor.R, starColor.G, starColor.B, (byte)25));
        Raylib.DrawCircle((int)starSx, (int)starSy, starRadius + 14f * z, new Color(starColor.R, starColor.G, starColor.B, (byte)60));
        Raylib.DrawCircle((int)starSx, (int)starSy, starRadius, starColor);
        Raylib.DrawCircle((int)starSx, (int)starSy, starRadius * 0.5f, new Color((byte)255, (byte)255, (byte)255, (byte)200));

        string systemTitle = system != null ? system.Name : "Unknown system";
        int titleW = UiText.MeasureText(systemTitle, 28);
        UiText.DrawText(systemTitle, cx - titleW / 2, 24, 28, Color.WHITE);

        LoadedPlanet[] planets = ResolvePlanets(system);

        int n = planets.Length;
        if (n > 0)
        {
            EnsureOrbitPhasesMatch(system, n);
            _orbitElapsedSeconds += Raylib.GetFrameTime();

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
                float aPx = MapSemiMajorAxisToPixels(p.SemiMajorAxisAu, minAu, maxAu, viewWidth, screenHeight) * z;
                float e = Math.Clamp(p.Eccentricity, 0f, 0.95f);
                float omega = p.ArgumentOfPeriapsisRad;

                DrawEllipticalOrbit(starSx, starSy, aPx, e, omega, dim);

                float meanAnomalyRad = WrapAngle0ToTau(
                    _meanAnomalyAtEpochRad[i] + _meanMotionRadPerSec[i] * _orbitElapsedSeconds);
                float nu = MeanAnomalyToTrueAnomaly(meanAnomalyRad, e);
                EllipseRadiusAndWorldOffset(aPx, e, omega, nu, out float worldPx, out float worldPy);

                float psx = starSx + worldPx;
                float psy = starSy + worldPy;

                float pr = (6 + (i % 3)) * z;
                Color pc = p.SurfaceColor;
                Raylib.DrawCircle((int)psx, (int)psy, pr + 2f * z, new Color(pc.R, pc.G, pc.B, (byte)100));
                Raylib.DrawCircle((int)psx, (int)psy, pr, pc);

                string label = p.Name;
                int fs = 16;
                int lw = UiText.MeasureText(label, fs);
                UiText.DrawText(label, (int)psx - lw / 2, (int)(psy - pr - 20f * z), fs, Color.LIGHTGRAY);
            }
        }

        DrawShipOnly(cx, cy, screenHeight, ship);
        DrawPlanetList(viewWidth, screenHeight, planets);
    }

    /// <summary>
    /// Planet names on the right side of the tactical view (inside the main view, not the HUD panel).
    /// </summary>
    private void DrawPlanetList(int viewWidth, int screenHeight, LoadedPlanet[] planets)
    {
        if (!_planetListVisible || planets.Length == 0)
        {
            return;
        }

        const int frameInset = 20;
        const int gapFromFrame = 8;
        const int listWidth = 280;
        const int padX = 14;
        const int padY = 12;
        const int titleFontSize = 28;
        const int rowFontSize = 24;
        const int rowHeight = 34;
        const int dotRadius = 8;
        const int titleBlock = 42;
        const int footerReserve = 58;

        int listRight = viewWidth - frameInset - gapFromFrame;
        int listLeft = listRight - listWidth;
        int listTop = 52;
        int maxBottom = screenHeight - footerReserve;
        int innerHeight = maxBottom - listTop - padY * 2;
        int maxRows = Math.Max(1, innerHeight / rowHeight);
        bool truncated = planets.Length > maxRows;
        int nameRows = truncated ? maxRows - 1 : planets.Length;
        int listHeight = padY * 2 + titleBlock + nameRows * rowHeight + (truncated ? rowHeight : 0);

        if (listTop + listHeight > maxBottom)
        {
            listHeight = maxBottom - listTop;
        }

        Raylib.DrawRectangle(listLeft, listTop, listWidth, listHeight, new Color(12, 14, 26, 230));
        Raylib.DrawRectangleLines(listLeft, listTop, listWidth, listHeight, new Color(70, 80, 115, 255));

        int textX = listLeft + padX;
        int y = listTop + padY;
        UiText.DrawText("Planets", textX, y, titleFontSize, new Color(180, 190, 220, 255));
        y += titleBlock;

        for (int i = 0; i < nameRows; i++)
        {
            LoadedPlanet p = planets[i];
            int dotCx = textX + dotRadius;
            int rowCy = y + rowHeight / 2;
            Raylib.DrawCircle(dotCx, rowCy, dotRadius + 1, new Color(p.SurfaceColor.R, p.SurfaceColor.G, p.SurfaceColor.B, (byte)120));
            Raylib.DrawCircle(dotCx, rowCy, dotRadius, p.SurfaceColor);
            UiText.DrawText(p.Name, textX + dotRadius * 2 + 10, y + 5, rowFontSize, Color.LIGHTGRAY);
            y += rowHeight;
        }

        if (truncated)
        {
            int more = planets.Length - nameRows;
            UiText.DrawText($"+{more} more", textX, y + 5, rowFontSize, new Color(130, 140, 170, 255));
        }
    }

    private const int OverviewMapOrbitSegments = 40;

    public void DrawOverviewMap(
        int panelX,
        int topY,
        int width,
        int height,
        StarSystem? system,
        Vector2 shipWorldPosition,
        int mainViewWidth,
        int mainViewHeight)
    {
        Raylib.DrawRectangle(panelX, topY, width, height, new Color(14, 16, 28, 255));
        Raylib.DrawRectangleLines(panelX, topY, width, height, new Color(55, 65, 95, 255));

        LoadedPlanet[] planets = ResolvePlanets(system);
        int n = planets.Length;
        if (n == 0)
        {
            UiText.DrawText("No chart data", panelX + 10, topY + height / 2 - 7, 16, Color.GRAY);
            return;
        }

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

        float maxDist = 1f;
        var planetWorld = new (float X, float Y)[n];
        var semiMajorPx = new float[n];

        for (int i = 0; i < n; i++)
        {
            LoadedPlanet p = planets[i];
            float aPx = MapSemiMajorAxisToPixels(p.SemiMajorAxisAu, minAu, maxAu, mainViewWidth, mainViewHeight);
            semiMajorPx[i] = aPx;
            float e = Math.Clamp(p.Eccentricity, 0f, 0.95f);
            float meanAnomalyRad = WrapAngle0ToTau(
                _meanAnomalyAtEpochRad[i] + _meanMotionRadPerSec[i] * _orbitElapsedSeconds);
            float nu = MeanAnomalyToTrueAnomaly(meanAnomalyRad, e);
            EllipseRadiusAndWorldOffset(aPx, e, p.ArgumentOfPeriapsisRad, nu, out float wx, out float wy);
            planetWorld[i] = (wx, wy);
            float d = MathF.Sqrt(wx * wx + wy * wy);
            if (d > maxDist)
            {
                maxDist = d;
            }

            float apo = aPx * (1f + e);
            if (apo > maxDist)
            {
                maxDist = apo;
            }
        }

        float shipLen = shipWorldPosition.Length();
        if (shipLen > maxDist)
        {
            maxDist = shipLen;
        }

        float margin = 10f;
        float fitRadius = MathF.Min(width, height) * 0.5f - margin;
        if (fitRadius < 8f)
        {
            fitRadius = 8f;
        }

        float scale = fitRadius / maxDist;

        float mapCx = panelX + width * 0.5f;
        float mapCy = topY + height * 0.5f;

        Color orbitDim = new Color(55, 65, 95, 140);

        for (int i = 0; i < n; i++)
        {
            LoadedPlanet p = planets[i];
            float e = Math.Clamp(p.Eccentricity, 0f, 0.95f);
            DrawEllipticalOrbitScaled(mapCx, mapCy, scale, semiMajorPx[i], e, p.ArgumentOfPeriapsisRad, orbitDim);
        }

        Color starColor = system?.StarColor ?? new Color(255, 220, 160, 255);
        Raylib.DrawCircle((int)mapCx, (int)mapCy, 6, new Color(starColor.R, starColor.G, starColor.B, (byte)90));
        Raylib.DrawCircle((int)mapCx, (int)mapCy, 4, starColor);
        Raylib.DrawCircle((int)mapCx, (int)mapCy, 2, new Color((byte)255, (byte)255, (byte)255, (byte)220));

        for (int i = 0; i < n; i++)
        {
            float sx = mapCx + planetWorld[i].X * scale;
            float sy = mapCy + planetWorld[i].Y * scale;
            Color pc = planets[i].SurfaceColor;
            Raylib.DrawCircle((int)sx, (int)sy, 4, new Color(pc.R, pc.G, pc.B, (byte)200));
            Raylib.DrawCircle((int)sx, (int)sy, 2, pc);
        }

        float shipScreenX = mapCx + shipWorldPosition.X * scale;
        float shipScreenY = mapCy + shipWorldPosition.Y * scale;
        Raylib.DrawCircleLines((int)shipScreenX, (int)shipScreenY, 6, Color.SKYBLUE);
        Raylib.DrawCircle((int)shipScreenX, (int)shipScreenY, 3, new Color(0, 230, 255, 255));

        const int labelSize = 12;
        UiText.DrawText("Overview", panelX + 8, topY + 4, labelSize, new Color(160, 175, 210, 255));
    }

    private static void DrawEllipticalOrbitScaled(
        float centerScreenX,
        float centerScreenY,
        float worldToMapScale,
        float semiMajorAxisPx,
        float eccentricity,
        float argumentOfPeriapsisRad,
        Color color)
    {
        float prevX = 0f;
        float prevY = 0f;
        bool hasPrev = false;

        for (int s = 0; s <= OverviewMapOrbitSegments; s++)
        {
            float nu = s * MathF.Tau / OverviewMapOrbitSegments;
            EllipseRadiusAndWorldOffset(semiMajorAxisPx, eccentricity, argumentOfPeriapsisRad, nu, out float wx, out float wy);
            float sx = centerScreenX + wx * worldToMapScale;
            float sy = centerScreenY + wy * worldToMapScale;

            if (hasPrev)
            {
                Raylib.DrawLine((int)prevX, (int)prevY, (int)sx, (int)sy, color);
            }

            prevX = sx;
            prevY = sy;
            hasPrev = true;
        }
    }

    private static void DrawShipOnly(int cx, int cy, int screenHeight, IShip ship)
    {
        bool forwardThrust = ship.ManeuverThrustForward;
        bool reverseThrust = ship.ManeuverThrustReverse;
        ShipRenderer.Draw(cx, cy, ship.Rotation, forwardThrust, reverseThrust);

        // Game.DrawStarSystemView draws a 20px bottom frame after this; keep text fully above it.
        const int bottomFrameThickness = 20;
        const int hudGapAboveFrame = 8;
        const int helpFontSize = 16;
        const int titleFontSize = 22;
        const int lineGap = 6;

        int helpY = screenHeight - bottomFrameThickness - hudGapAboveFrame - helpFontSize;
        int titleY = helpY - lineGap - titleFontSize;

        UiText.DrawText("STAR SYSTEM", 20, titleY, titleFontSize, Color.SKYBLUE);
        UiText.DrawText(
            "A/D or arrows: turn | W/S: thrust / reverse | P: planet list | ESC: Canopy | X: Quit",
            20,
            helpY,
            helpFontSize,
            Color.YELLOW);
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

    private void EnsureOrbitPhasesMatch(StarSystem? system, int planetCount)
    {
        string? id = system?.Id;
        if (_meanAnomalyAtEpochRad.Length == planetCount
            && _meanMotionRadPerSec.Length == planetCount
            && id == _orbitPhasesSystemId)
        {
            return;
        }

        ResampleOrbitPhases(system);
    }

    private void ResampleOrbitPhases(StarSystem? system)
    {
        LoadedPlanet[] planets = ResolvePlanets(system);
        int n = planets.Length;
        _orbitPhasesSystemId = system?.Id;
        _orbitElapsedSeconds = 0f;
        if (n == 0)
        {
            _meanAnomalyAtEpochRad = Array.Empty<float>();
            _meanMotionRadPerSec = Array.Empty<float>();
            return;
        }

        _meanAnomalyAtEpochRad = new float[n];
        _meanMotionRadPerSec = new float[n];
        for (int i = 0; i < n; i++)
        {
            float e = Math.Clamp(planets[i].Eccentricity, 0f, 0.95f);
            float nu0 = (float)(Random.Shared.NextDouble() * MathF.Tau);
            _meanAnomalyAtEpochRad[i] = TrueAnomalyToMeanAnomaly(nu0, e);

            float aAu = planets[i].SemiMajorAxisAu;
            float orbitalPeriodYears = MathF.Pow(aAu, 1.5f);
            float orbitalPeriodSeconds = orbitalPeriodYears * SimulatedSecondsPerEarthYear;
            if (orbitalPeriodSeconds < 1e-4f)
            {
                orbitalPeriodSeconds = 1e-4f;
            }

            _meanMotionRadPerSec[i] = MathF.Tau / orbitalPeriodSeconds;
        }
    }

    private static float WrapAngle0ToTau(float rad)
    {
        rad %= MathF.Tau;
        if (rad < 0f)
        {
            rad += MathF.Tau;
        }

        return rad;
    }

    /// <summary>
    /// Converts mean anomaly M to true anomaly ν (eccentric orbit, radians).
    /// </summary>
    private static float MeanAnomalyToTrueAnomaly(float meanAnomalyRad, float eccentricity)
    {
        float e = eccentricity;
        float E = MeanAnomalyToEccentricAnomaly(meanAnomalyRad, e);
        return EccentricAnomalyToTrueAnomaly(E, e);
    }

    private static float MeanAnomalyToEccentricAnomaly(float meanAnomalyRad, float eccentricity)
    {
        float e = eccentricity;
        float M = WrapAngle0ToTau(meanAnomalyRad);
        if (e < 1e-6f)
        {
            return M;
        }

        float E = M;
        for (int k = 0; k < 16; k++)
        {
            float next = M + e * MathF.Sin(E);
            if (MathF.Abs(next - E) < 1e-7f)
            {
                return next;
            }

            E = next;
        }

        return E;
    }

    private static float EccentricAnomalyToTrueAnomaly(float eccentricAnomalyRad, float eccentricity)
    {
        float e = eccentricity;
        float E = eccentricAnomalyRad;
        float cosE = MathF.Cos(E);
        float sinE = MathF.Sin(E);
        float denom = 1f - e * cosE;
        if (MathF.Abs(denom) < 1e-7f)
        {
            denom = denom >= 0f ? 1e-7f : -1e-7f;
        }

        float cosNu = (cosE - e) / denom;
        float sinNu = MathF.Sqrt(1f - e * e) * sinE / denom;
        return MathF.Atan2(sinNu, cosNu);
    }

    /// <summary>
    /// Converts true anomaly ν to mean anomaly M (eccentric orbit, radians).
    /// </summary>
    private static float TrueAnomalyToMeanAnomaly(float trueAnomalyRad, float eccentricity)
    {
        float e = eccentricity;
        float cosNu = MathF.Cos(trueAnomalyRad);
        float sinNu = MathF.Sin(trueAnomalyRad);
        float denom = 1f + e * cosNu;
        if (MathF.Abs(denom) < 1e-7f)
        {
            denom = denom >= 0f ? 1e-7f : -1e-7f;
        }

        float cosE = (e + cosNu) / denom;
        float sinE = MathF.Sqrt(1f - e * e) * sinNu / denom;
        float E = MathF.Atan2(sinE, cosE);
        return E - e * MathF.Sin(E);
    }

    private LoadedPlanet[] ResolvePlanets(StarSystem? system)
    {
        if (system == null)
        {
            return Array.Empty<LoadedPlanet>();
        }

        string? id = system.Id;
        if (string.IsNullOrWhiteSpace(id))
        {
            return Array.Empty<LoadedPlanet>();
        }

        string key = id.Trim().ToLowerInvariant();
        if (_planetsByStarSystemId.TryGetValue(key, out LoadedPlanet[]? planets))
        {
            return planets;
        }

        return Array.Empty<LoadedPlanet>();
    }

    private static int Mod(int x, int m)
    {
        int r = x % m;
        return r < 0 ? r + m : r;
    }
}
