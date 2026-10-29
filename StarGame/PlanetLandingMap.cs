using Raylib_cs;

namespace StarflightGame;

/// <summary>
/// Classic Starflight landing aid: equirectangular topographic map plus a movable site cursor.
/// </summary>
public sealed class PlanetLandingMap
{
    public const int MapColumns = 96;
    public const int MapRows = 48;

    private readonly Color[] _cells = new Color[MapColumns * MapRows];
    private readonly bool[] _liquid = new bool[MapColumns * MapRows];

    public Color[] Legend { get; init; } = Array.Empty<Color>();
    public float CursorLongitude { get; private set; } = 180f;
    public float CursorLatitude { get; private set; } = 12f;
    public bool AutoApproach { get; set; }
    public Rectangle MapScreenRect { get; set; }

    public static PlanetLandingMap FromPlanet(Planet planet)
    {
        var map = new PlanetLandingMap
        {
            Legend = planet.CreateAltitudeLegend()
        };

        for (int y = 0; y < MapRows; y++)
        {
            float lat = 90f - (y + 0.5f) / MapRows * 180f;
            for (int x = 0; x < MapColumns; x++)
            {
                float lon = (x + 0.5f) / MapColumns * 360f;
                planet.SampleSurface(lon, lat, out Color color, out _, out bool liquid);
                int index = y * MapColumns + x;
                map._cells[index] = color;
                map._liquid[index] = liquid;
            }
        }

        return map;
    }

    public Color ColorAt(int column, int row)
    {
        if ((uint)column >= MapColumns || (uint)row >= MapRows)
        {
            return Color.BLACK;
        }

        return _cells[row * MapColumns + column];
    }

    public bool CursorOnLiquid
    {
        get
        {
            CellAtCursor(out int column, out int row);
            return _liquid[row * MapColumns + column];
        }
    }

    public string CoordinateLabel
    {
        get
        {
            int lat = (int)MathF.Round(CursorLatitude);
            int lon = (int)MathF.Round(CursorLongitude);
            if (lon >= 360)
            {
                lon = 0;
            }

            string ns = lat >= 0 ? "N" : "S";
            return $"LAT {Math.Abs(lat):00}{ns}  LON {lon:000}";
        }
    }

    public void MoveCursor(float deltaLongitude, float deltaLatitude)
    {
        CursorLatitude = Math.Clamp(CursorLatitude + deltaLatitude, -88f, 88f);
        CursorLongitude = CursorLongitude + deltaLongitude;
        while (CursorLongitude < 0f)
        {
            CursorLongitude += 360f;
        }

        while (CursorLongitude >= 360f)
        {
            CursorLongitude -= 360f;
        }
    }

    public void SetCursorFromMapPixel(float pixelX, float pixelY)
    {
        Rectangle rect = MapScreenRect;
        if (rect.Width <= 1f || rect.Height <= 1f)
        {
            return;
        }

        float u = Math.Clamp((pixelX - rect.X) / rect.Width, 0f, 0.999f);
        float v = Math.Clamp((pixelY - rect.Y) / rect.Height, 0f, 0.999f);
        CursorLongitude = u * 360f;
        CursorLatitude = 90f - v * 180f;
        CursorLatitude = Math.Clamp(CursorLatitude, -88f, 88f);
    }

    public void CursorMapPosition(int mapX, int mapY, int mapWidth, int mapHeight, out int pixelX, out int pixelY)
    {
        float u = CursorLongitude / 360f;
        float v = (90f - CursorLatitude) / 180f;
        pixelX = mapX + (int)(u * mapWidth);
        pixelY = mapY + (int)(v * mapHeight);
    }

    private void CellAtCursor(out int column, out int row)
    {
        column = Math.Clamp((int)(CursorLongitude / 360f * MapColumns), 0, MapColumns - 1);
        row = Math.Clamp((int)((90f - CursorLatitude) / 180f * MapRows), 0, MapRows - 1);
    }
}
