using Raylib_cs;
using System;

namespace StarflightGame.Views;

public interface IStarSystemInteriorView
{
    void Draw(StarSystem? system, int viewWidth, int screenHeight);
}


/// <summary>
/// Tactical view of the local star system: central star and a fixed set of named planets on static orbits.
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

    public void Draw(StarSystem? system, int viewWidth, int screenHeight)
    {
        int cx = viewWidth / 2;
        int cy = screenHeight / 2;

        Raylib.DrawRectangle(0, 0, viewWidth, screenHeight, new Color(8, 10, 22, 255));

        for (int i = 0; i < 80; i++)
        {
            int sx = (i * 97) % viewWidth;
            int sy = (i * 53) % screenHeight;
            Raylib.DrawPixel(sx, sy, new Color(40, 45, 70, 255));
        }

        Color starColor = system?.StarColor ?? new Color(255, 220, 160, 255);
        const int starRadius = 36;

        Raylib.DrawCircle(cx, cy, starRadius + 28, new Color(starColor.R, starColor.G, starColor.B, (byte)25));
        Raylib.DrawCircle(cx, cy, starRadius + 14, new Color(starColor.R, starColor.G, starColor.B, (byte)60));
        Raylib.DrawCircle(cx, cy, starRadius, starColor);
        Raylib.DrawCircle(cx, cy, starRadius / 2, new Color((byte)255, (byte)255, (byte)255, (byte)200));

        string systemTitle = system != null ? system.Name : "Unknown system";
        int titleW = Raylib.MeasureText(systemTitle, 28);
        Raylib.DrawText(systemTitle, cx - titleW / 2, 24, 28, Color.WHITE);

        for (int i = 0; i < PlanetCount; i++)
        {
            float orbitR = 70f + i * 38f;
            float angle = i * (MathF.Tau / PlanetCount);
            float px = cx + MathF.Cos(angle) * orbitR;
            float py = cy + MathF.Sin(angle) * orbitR;

            Color dim = new Color(60, 65, 90, 120);
            Raylib.DrawCircleLines(cx, cy, (int)orbitR, dim);

            int pr = 6 + (i % 3);
            Color pc = PlanetColors[i % PlanetColors.Length];
            Raylib.DrawCircle((int)px, (int)py, pr + 2, new Color(pc.R, pc.G, pc.B, (byte)100));
            Raylib.DrawCircle((int)px, (int)py, pr, pc);

            string label = PlanetNames[i];
            int fs = 16;
            int lw = Raylib.MeasureText(label, fs);
            Raylib.DrawText(label, (int)px - lw / 2, (int)py - pr - 20, fs, Color.LIGHTGRAY);
        }

        Raylib.DrawText("STAR SYSTEM", 20, screenHeight - 56, 22, Color.SKYBLUE);
        Raylib.DrawText("ESC: Canopy  |  X: Quit", 20, screenHeight - 28, 16, Color.YELLOW);
    }
}
