using Raylib_cs;
using System;

namespace StarflightGame;

internal static class EncounterStarfield
{
    public static void Draw(int viewWidth, int viewHeight)
    {
        Random starRandom = new Random(42);
        for (int i = 0; i < 100; i++)
        {
            int x = starRandom.Next(0, viewWidth);
            int y = starRandom.Next(0, viewHeight);
            float brightness = (float)(starRandom.NextDouble() * 0.5 + 0.5);
            Color starColor = new Color(
                (byte)(255 * brightness),
                (byte)(255 * brightness),
                (byte)(255 * brightness),
                (byte)255);
            Raylib.DrawPixel(x, y, starColor);
        }
    }
}
