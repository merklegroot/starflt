using Raylib_cs;
using System.Numerics;
using StarflightGame;

namespace StarflightGame.Views;

/// <summary>
/// Top-down local terrain around a landing site, in the spirit of classic Starflight planetfall.
/// </summary>
public sealed class PlanetSurfaceView
{
    public const int TileSize = 16;
    public const float DegreesPerTile = 0.22f;

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
        int viewHeight,
        PlanetSurfaceDeposits? deposits = null)
    {
        int camPixX;
        int camPixY;
        DrawWorldLockedTerrain(planet, cameraLongitude, cameraLatitude, viewWidth, viewHeight, deposits, out camPixX, out camPixY);

        WorldToScreen(
            shipLongitude,
            shipLatitude,
            camPixX,
            camPixY,
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
            DrawTerrainVehicle(viewWidth / 2, viewHeight / 2, tvHeading);
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

    /// <summary>
    /// Tiles are locked to a lon/lat grid so each cell keeps a stable color.
    /// The camera scrolls that grid in pixels instead of resampling the sphere every frame,
    /// which is what made color bands flicker while driving.
    /// </summary>
    private static void DrawWorldLockedTerrain(
        Planet planet,
        float cameraLon,
        float cameraLat,
        int viewWidth,
        int viewHeight,
        PlanetSurfaceDeposits? deposits,
        out int camPixX,
        out int camPixY)
    {
        int cols = viewWidth / TileSize + 4;
        int rows = viewHeight / TileSize + 4;
        float camTileX = cameraLon / DegreesPerTile;
        float camTileY = cameraLat / DegreesPerTile;
        camPixX = (int)MathF.Floor(camTileX * TileSize);
        camPixY = (int)MathF.Floor(camTileY * TileSize);
        int originTx = (int)MathF.Floor(camTileX) - cols / 2;
        int originTy = (int)MathF.Floor(camTileY) + rows / 2;
        int centerX = viewWidth / 2;
        int centerY = viewHeight / 2;

        for (int row = 0; row < rows; row++)
        {
            int ty = originTy - row;
            float sampleLat = Math.Clamp((ty + 0.5f) * DegreesPerTile, -89.5f, 89.5f);
            int top = centerY - (ty + 1) * TileSize + camPixY;

            for (int col = 0; col < cols; col++)
            {
                int tx = originTx + col;
                float sampleLon = WrapLon((tx + 0.5f) * DegreesPerTile);
                planet.SampleSurface(sampleLon, sampleLat, out Color color, out _, out bool liquid);
                if (liquid)
                {
                    color = Shade(color, 0.82f);
                }

                int left = centerX + tx * TileSize - camPixX;
                Raylib.DrawRectangle(left, top, TileSize, TileSize, color);

                if (deposits != null && deposits.TryGet(tx, ty, out SurfaceMineralDeposit mineral))
                {
                    MineralIconRenderer.Draw(left + TileSize / 2, top + TileSize / 2, mineral.Color);
                }
            }
        }
    }

    private static void WorldToScreen(
        float lon,
        float lat,
        int camPixX,
        int camPixY,
        int viewWidth,
        int viewHeight,
        out int x,
        out int y)
    {
        int entPixX = (int)MathF.Floor(lon / DegreesPerTile * TileSize);
        int entPixY = (int)MathF.Floor(lat / DegreesPerTile * TileSize);
        x = viewWidth / 2 + entPixX - camPixX;
        y = viewHeight / 2 - (entPixY - camPixY);
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
