using Raylib_cs;
using System.Numerics;

namespace StarflightGame;

public class StarSystem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public Vector2 Position { get; set; }
    public Color StarColor { get; set; }

    public StarSystem(string id, string name, Vector2 position, Color starColor)
    {
        Id = id;
        Name = name;
        Position = position;
        StarColor = starColor;
    }
}
