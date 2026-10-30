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

    public void Draw(
        Planet planet,
        float cameraLongitude,
        float cameraLatitude,
        float shipLongitude,
        float shipLatitude,
        float shipHeading,
        bool disembarked,
        float tvLongitude,
        float tvLatitude,
        float tvHeading,
        int viewWidth,
        int viewHeight)
    {
        int cols = viewWidth / TileSize + 3;
        int rows = viewHeight / TileSize + 3;
        int originX = viewWidth / 2 - cols * TileSize / 2;
        int originY = viewHeight / 2 - rows * TileSize / 2;
        int centerCol = cols / 2;
        int centerRow = rows / 2;

        for (int row = 0; row < rows; row++)
        {
            float lat = cameraLatitude + (centerRow - row) * DegreesPerTile;
            lat = Math.Clamp(lat, -89.5f, 89.5f);
            for (int col = 0; col < cols; col++)
            {
                float lon = WrapLon(cameraLongitude + (col - centerCol) * DegreesPerTile);
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

        WorldToScreen(
            shipLongitude,
            shipLatitude,
            cameraLongitude,
            cameraLatitude,
            viewWidth,
            viewHeight,
            out int shipX,
            out int shipY);
        if (shipX > -40 && shipX < viewWidth + 40 && shipY > -40 && shipY < viewHeight + 40)
        {
            Raylib.DrawCircle(shipX, shipY, 18, new Color(0, 0, 0, 90));
            ShipRenderer.Draw(shipX, shipY, shipHeading, scale: 1.15f);
        }

        if (disembarked)
        {
            WorldToScreen(
                tvLongitude,
                tvLatitude,
                cameraLongitude,
                cameraLatitude,
                viewWidth,
                viewHeight,
                out int tvX,
                out int tvY);
            DrawTerrainVehicle(tvX, tvY, tvHeading);
        }

        const int titleSize = 22;
        UiText.DrawTextCenteredAtXClamped(
            disembarked ? "TERRAIN VEHICLE" : "PLANETFALL",
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

    private static void WorldToScreen(
        float lon,
        float lat,
        float cameraLon,
        float cameraLat,
        int viewWidth,
        int viewHeight,
        out int x,
        out int y)
    {
        float dLon = lon - cameraLon;
        while (dLon > 180f)
        {
            dLon -= 360f;
        }

        while (dLon < -180f)
        {
            dLon += 360f;
        }

        x = viewWidth / 2 + (int)MathF.Round(dLon / DegreesPerTile * TileSize);
        y = viewHeight / 2 - (int)MathF.Round((lat - cameraLat) / DegreesPerTile * TileSize);
    }

    private static void DrawTerrainVehicle(int cx, int cy, float heading)
    {
        Vector2 Body(float right, float forward)
        {
            return new Vector2(
                cx + forward * MathF.Cos(heading) - right * MathF.Sin(heading),
                cy + forward * MathF.Sin(heading) + right * MathF.Cos(heading));
        }

        Vector2 nose = Body(0f, 9f);
        Vector2 rearLeft = Body(-7f, -7f);
        Vector2 rearRight = Body(7f, -7f);
        Vector2 midLeft = Body(-7f, 3f);
        Vector2 midRight = Body(7f, 3f);

        Color hull = new Color(210, 170, 60, 255);
        Color shade = new Color(140, 100, 30, 255);
        Color cabin = new Color(80, 200, 220, 255);

        Raylib.DrawTriangle(midLeft, rearLeft, rearRight, shade);
        Raylib.DrawTriangle(midLeft, rearRight, midRight, shade);
        Raylib.DrawTriangle(nose, midLeft, midRight, hull);

        Vector2 wheelL = Body(-8f, -2f);
        Vector2 wheelR = Body(8f, -2f);
        Raylib.DrawCircle((int)wheelL.X, (int)wheelL.Y, 3, new Color(40, 40, 40, 255));
        Raylib.DrawCircle((int)wheelR.X, (int)wheelR.Y, 3, new Color(40, 40, 40, 255));

        Vector2 canopy = Body(0f, 2f);
        Raylib.DrawCircle((int)canopy.X, (int)canopy.Y, 3, cabin);
        Raylib.DrawLineEx(Body(0f, -4f), nose, 2f, Color.WHITE);
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
