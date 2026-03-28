using Raylib_cs;
using System;
using System.Numerics;

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
    public const int PlanetCount = 8;

    private static readonly string[] PlanetNames =
    {
        "Veridian",
        "Crimson Reach",
        "Echo",
        "Glass",
        "Keth",
        "Mire",
        "Nadir",
        "Solstice"
    };

    private static readonly Color[] PlanetColors =
    {
        new Color(80, 200, 120, 255),
        new Color(200, 90, 70, 255),
        new Color(180, 180, 220, 255),
        new Color(140, 200, 240, 255),
        new Color(200, 160, 80, 255),
        new Color(100, 140, 110, 255),
        new Color(120, 100, 160, 255),
        new Color(230, 210, 160, 255)
    };

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

        for (int i = 0; i < PlanetCount; i++)
        {
            float orbitR = 70f + i * 38f;
            float angle = i * (MathF.Tau / PlanetCount);
            float worldPx = MathF.Cos(angle) * orbitR;
            float worldPy = MathF.Sin(angle) * orbitR;

            float psx = cx + worldPx - spx;
            float psy = cy + worldPy - spy;

            Color dim = new Color(60, 65, 90, 120);
            Raylib.DrawCircleLines((int)starSx, (int)starSy, (int)orbitR, dim);

            int pr = 6 + (i % 3);
            Color pc = PlanetColors[i % PlanetColors.Length];
            Raylib.DrawCircle((int)psx, (int)psy, pr + 2, new Color(pc.R, pc.G, pc.B, (byte)100));
            Raylib.DrawCircle((int)psx, (int)psy, pr, pc);

            string label = PlanetNames[i];
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

    private static int Mod(int x, int m)
    {
        int r = x % m;
        return r < 0 ? r + m : r;
    }
}
