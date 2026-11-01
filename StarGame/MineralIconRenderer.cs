using System.IO;
using System.Numerics;
using Raylib_cs;

namespace StarflightGame;

/// <summary>
/// Surface mineral outcropping: two chevron arrows crossing as an X (up-left and up-right).
/// </summary>
public static class MineralIconRenderer
{
    private const string TextureRelativePath = "Textures/ui/mineral-icon.png";

    /// <summary>Slightly larger than a surface tile so the X reads at a glance.</summary>
    private const float DisplaySize = 20f;

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

    public static void Draw(int centerX, int centerY, Color tint)
    {
        if (_textureLoaded)
        {
            Rectangle source = new Rectangle(0f, 0f, _texture.Width, _texture.Height);
            Rectangle dest = new Rectangle(centerX, centerY, DisplaySize, DisplaySize);
            Vector2 origin = new Vector2(DisplaySize * 0.5f, DisplaySize * 0.5f);
            Raylib.DrawTexturePro(_texture, source, dest, origin, 0f, tint);
        }
        else
        {
            DrawFallback(centerX, centerY, tint);
        }
    }

    private static void DrawFallback(int centerX, int centerY, Color color)
    {
        const float half = 7f;
        const float thick = 2.2f;
        Vector2 topLeft = new Vector2(centerX - half, centerY - half);
        Vector2 topRight = new Vector2(centerX + half, centerY - half);
        Vector2 bottomRight = new Vector2(centerX + half, centerY + half);
        Vector2 bottomLeft = new Vector2(centerX - half, centerY + half);
        Raylib.DrawLineEx(topLeft, bottomRight, thick, color);
        Raylib.DrawLineEx(topRight, bottomLeft, thick, color);
        Raylib.DrawLineEx(topLeft, new Vector2(topLeft.X + 4f, topLeft.Y), thick, color);
        Raylib.DrawLineEx(topLeft, new Vector2(topLeft.X, topLeft.Y + 4f), thick, color);
        Raylib.DrawLineEx(topRight, new Vector2(topRight.X - 4f, topRight.Y), thick, color);
        Raylib.DrawLineEx(topRight, new Vector2(topRight.X, topRight.Y + 4f), thick, color);
    }
}
