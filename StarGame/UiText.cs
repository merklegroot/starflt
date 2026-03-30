using System.IO;
using System.Numerics;
using Raylib_cs;

namespace StarflightGame;

/// <summary>
/// Renders UI text with a readable TTF instead of Raylib's default bitmap font.
/// </summary>
public static class UiText
{
    private static Font _font;
    private static bool _useCustomFont = false;
    private const float Spacing = 0f;
    private const int AtlasFontSize = 128;

    public static void Load()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fonts", "OpenSans.ttf");
        if (File.Exists(path))
        {
            _font = Raylib.LoadFontEx(path, AtlasFontSize, null, 0);
            Raylib.SetTextureFilter(_font.Texture, TextureFilter.TEXTURE_FILTER_BILINEAR);
            _useCustomFont = true;
        }
        else
        {
            _font = Raylib.GetFontDefault();
            _useCustomFont = false;
        }
    }

    public static void Unload()
    {
        if (_useCustomFont)
        {
            Raylib.UnloadFont(_font);
            _useCustomFont = false;
        }
    }

    public static void DrawText(string text, int x, int y, int fontSize, Color color)
    {
        Raylib.DrawTextEx(_font, text, new Vector2(x, y), fontSize, Spacing, color);
    }

    public static int MeasureText(string text, int fontSize)
    {
        Vector2 size = Raylib.MeasureTextEx(_font, text, fontSize, Spacing);
        return (int)MathF.Ceiling(size.X);
    }

    /// <summary>
    /// Exact glyph bounds from the same path as <see cref="DrawText"/>; use for centering (avoid <see cref="MeasureText"/> ceiling bias).
    /// </summary>
    public static Vector2 MeasureTextSize(string text, int fontSize)
    {
        return Raylib.MeasureTextEx(_font, text, fontSize, Spacing);
    }

    /// <summary>
    /// Horizontally centers text on <paramref name="centerX"/> using float X from <see cref="MeasureTextSize"/> (matches DrawTextEx layout).
    /// </summary>
    public static void DrawTextCenteredAtX(string text, float centerX, float y, int fontSize, Color color)
    {
        Vector2 size = Raylib.MeasureTextEx(_font, text, fontSize, Spacing);
        float x = centerX - size.X * 0.5f;
        Raylib.DrawTextEx(_font, text, new Vector2(x, y), fontSize, Spacing, color);
    }

    /// <summary>
    /// Same as <see cref="DrawTextCenteredAtX"/> but clamps so the string stays within [minX, maxX] for its drawn width.
    /// </summary>
    public static void DrawTextCenteredAtXClamped(string text, float centerX, float y, int fontSize, Color color, float minX, float maxX)
    {
        Vector2 size = Raylib.MeasureTextEx(_font, text, fontSize, Spacing);
        float x = centerX - size.X * 0.5f;
        if (x < minX)
        {
            x = minX;
        }
        else if (x + size.X > maxX)
        {
            x = maxX - size.X;
        }

        Raylib.DrawTextEx(_font, text, new Vector2(x, y), fontSize, Spacing, color);
    }

    /// <summary>
    /// Centered text with a 1px outline (8 directions) for emphasis on busy backgrounds.
    /// </summary>
    public static void DrawTextCenteredAtXOutlined(string text, float centerX, float y, int fontSize, Color fillColor, Color outlineColor)
    {
        Vector2 size = Raylib.MeasureTextEx(_font, text, fontSize, Spacing);
        float baseX = centerX - size.X * 0.5f;

        for (int oy = -1; oy <= 1; oy++)
        {
            for (int ox = -1; ox <= 1; ox++)
            {
                if (ox == 0 && oy == 0)
                {
                    continue;
                }

                Raylib.DrawTextEx(_font, text, new Vector2(baseX + ox, y + oy), fontSize, Spacing, outlineColor);
            }
        }

        Raylib.DrawTextEx(_font, text, new Vector2(baseX, y), fontSize, Spacing, fillColor);
    }
}
