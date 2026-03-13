using Raylib_cs;
using System.Numerics;

namespace StarflightGame;

public class StarSystem
{
    public string Name { get; set; } = "";
    public Vector2 Position { get; set; }
    public Color StarColor { get; set; }

    public StarSystem(string name, Vector2 position, Color starColor)
    {
        Name = name;
        Position = position;
        StarColor = starColor;
    }
}
