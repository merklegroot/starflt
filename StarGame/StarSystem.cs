using Raylib_cs;
using System.Numerics;

namespace StarflightGame;

public class StarSystem
{
    public string Name { get; set; } = "";
    public Vector2 Position { get; set; }
    public Color StarColor { get; set; }
    public List<Planet> Planets { get; private set; }

    public StarSystem(string name, Vector2 position, Color starColor)
    {
        Name = name;
        Position = position;
        StarColor = starColor;
        Planets = new List<Planet>();
    }

    public void AddPlanet(Planet planet)
    {
        Planets.Add(planet);
    }
}
