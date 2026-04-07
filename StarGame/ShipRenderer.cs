using System.IO;
using Raylib_cs;
using System.Numerics;

namespace StarflightGame;

/// <summary>
/// Draws the player ship using the first frame of <c>tinyShip10.png</c> (horizontal sprite sheet; we only use the leftmost frame).
/// </summary>
public static class ShipRenderer
{
    private const string TextureRelativePath = "Textures/tiny-spaceships/tinyShip10.png";

    /// <summary>Frame width/height from Disruptor Art readme; 1 px horizontal gap between frames.</summary>
    private const float FrameWidth = 40f;

    private const float FrameHeight = 29f;

    /// <summary>Scales pixel art for visibility on the 1024×768 view.</summary>
    private const float DisplayScale = 1.25f;

    private static Texture2D _texture;

    private static bool _textureLoaded = false;

    public static void Load()
    {
        if (_textureLoaded)
        {
            return;
        }

        string path = Path.Combine(AppContext.BaseDirectory, TextureRelativePath);
        if (!File.Exists(path))
        {
            return;
        }

        if (LooksLikeGitLfsPointer(path))
        {
            Console.Error.WriteLine(
                "Ship texture not loaded: Tiny Spaceships PNGs are Git LFS files. Install Git LFS, run \"git lfs install\" and \"git lfs pull\" in the repo (see ASSETS.md).");
            return;
        }

        _texture = Raylib.LoadTexture(path);
        if (_texture.Id == 0 || _texture.Width <= 0 || _texture.Height <= 0)
        {
            return;
        }

        Raylib.SetTextureFilter(_texture, TextureFilter.TEXTURE_FILTER_POINT);
        _textureLoaded = true;
    }

    public static void Unload()
    {
        if (!_textureLoaded)
        {
            return;
        }

        Raylib.UnloadTexture(_texture);
        _textureLoaded = false;
    }

    public static void Draw(int centerX, int centerY, float rotation, bool forwardThrust = false, bool reverseThrust = false, float scale = 1f)
    {
        if (_textureLoaded)
        {
            DrawSprite(centerX, centerY, rotation, scale);
        }
        else
        {
            DrawProceduralFallback(centerX, centerY, rotation, forwardThrust, reverseThrust, scale);
        }
    }

    /// <summary>True when the file is still a Git LFS pointer (real PNG bytes were never fetched).</summary>
    private static bool LooksLikeGitLfsPointer(string path)
    {
        try
        {
            using StreamReader reader = new StreamReader(path);
            string? line = reader.ReadLine();
            return line != null
                && line.StartsWith("version https://git-lfs.github.com/spec/v1", StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private static void DrawSprite(int centerX, int centerY, float rotation, float scale)
    {
        Rectangle source = new Rectangle(0f, 0f, FrameWidth, FrameHeight);

        float dw = FrameWidth * DisplayScale * scale;
        float dh = FrameHeight * DisplayScale * scale;
        // DrawTexturePro: dest.x/y is the pivot in screen space; drawn top-left is dest - origin (raylib).
        Rectangle dest = new Rectangle(centerX, centerY, dw, dh);
        Vector2 origin = new Vector2(dw * 0.5f, dh * 0.5f);

        // Ship rotation is radians (screen space, +Y down); Raylib uses degrees, clockwise.
        float rotationDeg = rotation * (180.0f / MathF.PI);

        Raylib.DrawTexturePro(_texture, source, dest, origin, rotationDeg, Color.WHITE);
    }

    private static void DrawProceduralFallback(int centerX, int centerY, float rotation, bool forwardThrust, bool reverseThrust, float scale)
    {
        Vector2 center = new Vector2(centerX, centerY);
        float s = scale;

        Vector2[] baseShipPoints = new Vector2[]
        {
            new Vector2(centerX, centerY - 30f * s),
            new Vector2(centerX - 25f * s, centerY + 20f * s),
            new Vector2(centerX + 25f * s, centerY + 20f * s)
        };

        Vector2[] shipPoints = new Vector2[]
        {
            RotatePoint(baseShipPoints[0], center, rotation),
            RotatePoint(baseShipPoints[1], center, rotation),
            RotatePoint(baseShipPoints[2], center, rotation)
        };

        Raylib.DrawTriangle(shipPoints[0], shipPoints[1], shipPoints[2], Color.GRAY);
        Raylib.DrawTriangleLines(shipPoints[0], shipPoints[1], shipPoints[2], Color.WHITE);

        Vector2[] baseCockpitPoints = new Vector2[]
        {
            new Vector2(centerX, centerY - 15f * s),
            new Vector2(centerX - 8f * s, centerY - 5f * s),
            new Vector2(centerX + 8f * s, centerY - 5f * s)
        };

        Vector2[] cockpitPoints = new Vector2[]
        {
            RotatePoint(baseCockpitPoints[0], center, rotation),
            RotatePoint(baseCockpitPoints[1], center, rotation),
            RotatePoint(baseCockpitPoints[2], center, rotation)
        };
        Raylib.DrawTriangle(cockpitPoints[0], cockpitPoints[1], cockpitPoints[2], new Color(50, 100, 150, 200));

        Vector2[] enginePositions = new Vector2[]
        {
            new Vector2(centerX - 20f * s, centerY + 20f * s),
            new Vector2(centerX + 12f * s, centerY + 20f * s)
        };

        foreach (var enginePos in enginePositions)
        {
            Vector2 rotatedPos = RotatePoint(enginePos, center, rotation);
            Color engineBody = forwardThrust ? new Color(70, 70, 85, 255) : Color.DARKGRAY;
            int ew = (int)MathF.Round(8f * s);
            int eh = (int)MathF.Round(12f * s);
            Raylib.DrawRectangle((int)rotatedPos.X - ew / 2, (int)rotatedPos.Y - eh / 2, ew, eh, engineBody);
        }

        if (forwardThrust)
        {
            Vector2 aft = RotatePoint(new Vector2(centerX, centerY + 28f * s), center, rotation);
            Vector2 backward = new Vector2(-MathF.Sin(rotation), MathF.Cos(rotation));
            Vector2 flameTip = aft + backward * (18f * s);
            Raylib.DrawLine((int)aft.X, (int)aft.Y, (int)flameTip.X, (int)flameTip.Y, new Color(255, 180, 80, 220));
            Raylib.DrawLine((int)(aft.X - 3), (int)aft.Y, (int)(flameTip.X - 2), (int)(flameTip.Y + 2), new Color(255, 220, 120, 180));
            Raylib.DrawLine((int)(aft.X + 3), (int)aft.Y, (int)(flameTip.X + 2), (int)(flameTip.Y + 2), new Color(255, 220, 120, 180));
        }
        else if (reverseThrust)
        {
            Vector2 aft = RotatePoint(new Vector2(centerX, centerY + 28f * s), center, rotation);
            Vector2 forwardDir = new Vector2(MathF.Sin(rotation), -MathF.Cos(rotation));
            Vector2 puff = aft + forwardDir * (10f * s);
            Raylib.DrawLine((int)aft.X, (int)aft.Y, (int)puff.X, (int)puff.Y, new Color(150, 200, 255, 160));
        }

        Vector2 lineStart = RotatePoint(new Vector2(centerX - 15f * s, centerY + 5f * s), center, rotation);
        Vector2 lineEnd = RotatePoint(new Vector2(centerX + 15f * s, centerY + 5f * s), center, rotation);
        Raylib.DrawLine((int)lineStart.X, (int)lineStart.Y, (int)lineEnd.X, (int)lineEnd.Y, Color.DARKGRAY);
    }

    private static Vector2 RotatePoint(Vector2 point, Vector2 center, float angle)
    {
        float cos = MathF.Cos(angle);
        float sin = MathF.Sin(angle);
        float dx = point.X - center.X;
        float dy = point.Y - center.Y;
        return new Vector2(
            center.X + dx * cos - dy * sin,
            center.Y + dx * sin + dy * cos);
    }
}
