using Raylib_cs;
using System.Numerics;

namespace StarflightGame;

public class Mineral
{
    public string Name { get; set; } = "";
    public int Value { get; set; }
    public Vector2 Position { get; set; }
    public Color Color { get; set; }
    public bool Mined { get; set; } = false;

    public Mineral(string name, int value, Vector2 position, Color color)
    {
        Name = name;
        Value = value;
        Position = position;
        Color = color;
    }
}
