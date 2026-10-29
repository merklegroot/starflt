using Raylib_cs;
using System.Numerics;

namespace StarflightGame.Views;

/// <summary>
/// Top-down local terrain around a landing site, in the spirit of classic Starflight planetfall.
/// </summary>
public sealed class PlanetSurfaceView
{
    private const int TileSize = 16;
    private const float DegreesPerTile = 0.22f;

    public void Draw(Planet planet, float centerLongitude, float centerLatitude, float heading, int viewWidth, int viewHeight)
    {
        int cols = viewWidth / TileSize + 3;
        int rows = viewHeight / TileSize + 3;
        int originX = viewWidth / 2 - cols * TileSize / 2;
        int originY = viewHeight / 2 - rows * TileSize / 2;
        int centerCol = cols / 2;
        int centerRow = rows / 2;

        for (int row = 0; row < rows; row++)
        {
            float lat = centerLatitude + (centerRow - row) * DegreesPerTile;
            lat = Math.Clamp(lat, -89.5f, 89.5f);
            for (int col = 0; col < cols; col++)
            {
                float lon = WrapLon(centerLongitude + (col - centerCol) * DegreesPerTile);
                planet.SampleSurface(lon, lat, out Color color, out _, out bool liquid);
                if (liquid)
                {
                    color = Shade(color, 0.82f);
                }

                Raylib.DrawRectangle(
                    originX + col * TileSize,
                    originY + row * TileSize,
                    TileSize,
                    TileSize,
                    color);
            }
        }

        int shipX = viewWidth / 2;
        int shipY = viewHeight / 2;
        Raylib.DrawCircle(shipX, shipY, 16, new Color(0, 0, 0, 90));
        ShipRenderer.Draw(shipX, shipY, heading, scale: 1.15f);

        const int titleSize = 22;
        UiText.DrawTextCenteredAtXClamped(
            "PLANETFALL",
            viewWidth * 0.5f,
            22,
            titleSize,
            new Color(200, 205, 220, 255),
            12f,
            viewWidth - 12f);
        UiText.DrawTextCenteredAtXOutlined(
            planet.Name,
            viewWidth * 0.5f,
            22 + titleSize + 8,
            28,
            Color.WHITE,
            new Color(0, 0, 0, 200));
    }

    private static float WrapLon(float lon)
    {
        while (lon < 0f)
        {
            lon += 360f;
        }

        while (lon >= 360f)
        {
            lon -= 360f;
        }

        return lon;
    }

    private static Color Shade(Color color, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        return new Color(
            (byte)(color.R * amount),
            (byte)(color.G * amount),
            (byte)(color.B * amount),
            (byte)255);
    }
}
