using Raylib_cs;
using System.Numerics;
using System;
using System.IO;
using System.Text.Json;
using System.Reflection;

namespace StarflightGame;

public class StarMapView
{
    private List<StarSystem> _systems = LoadStarSystems();

    private Vector2 _cameraOffset = Vector2.Zero;
    private float _zoom = 1.0f;
    private const float MinZoom = 0.5f;
    private const float MaxZoom = 3.0f;

    private static List<StarSystem> LoadStarSystems()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "StarflightGame.starSystems.json";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
        }

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var starSystemData = JsonSerializer.Deserialize<List<StarSystemData>>(json, options);
        if (starSystemData == null)
        {
            throw new InvalidOperationException("Failed to deserialize star systems data");
        }

        var systems = new List<StarSystem>();
        foreach (var data in starSystemData)
        {
            var position = new Vector2(data.Position.X, data.Position.Y);
            var color = new Color(data.StarColor.R, data.StarColor.G, data.StarColor.B, data.StarColor.A);
            systems.Add(new StarSystem(data.Name, position, color));
        }

        return systems;
    }

    private class StarSystemData
    {
        public string Name { get; set; } = "";
        public Vector2Data Position { get; set; }
        public ColorData StarColor { get; set; }
    }

    private class Vector2Data
    {
        public float X { get; set; }
        public float Y { get; set; }
    }

    private class ColorData
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; }
    }

    public StarMapView()
    {
        // Generate planets for each system using PlanetGenerator
        var random = new Random();
        foreach (var system in _systems)
        {
            // Generate 2-6 planets per system
            int planetCount = random.Next(2, 7);
            
            for (int i = 0; i < planetCount; i++)
            {
                Planet planet = PlanetGenerator.GeneratePlanet(system.Name, system.Position, i, random);
                system.AddPlanet(planet);
            }
        }
    }

    public StarSystem? GetSystem(int index)
    {
        if (index >= 0 && index < _systems.Count)
            return _systems[index];
        return null;
    }

    public List<StarSystem> GetAllSystems()
    {
        return _systems;
    }

    public StarSystem? GetSystemAtPosition(Vector2 position)
    {
        const float systemRange = 30.0f;
        foreach (var system in _systems)
        {
            if (Vector2.Distance(position, system.Position) < systemRange)
            {
                return system;
            }
        }

        return null;
    }

    public void Update(Ship ship)
    {
        // Camera movement
        Vector2 movement = Vector2.Zero;
        float speed = 5.0f / _zoom;

        if (Raylib.IsKeyDown(KeyboardKey.KEY_W) || Raylib.IsKeyDown(KeyboardKey.KEY_UP))
            movement.Y -= speed;
        if (Raylib.IsKeyDown(KeyboardKey.KEY_S) || Raylib.IsKeyDown(KeyboardKey.KEY_DOWN))
            movement.Y += speed;
        if (Raylib.IsKeyDown(KeyboardKey.KEY_A) || Raylib.IsKeyDown(KeyboardKey.KEY_LEFT))
            movement.X -= speed;
        if (Raylib.IsKeyDown(KeyboardKey.KEY_D) || Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT))
            movement.X += speed;

        _cameraOffset += movement;

        // Zoom
        float wheelMove = Raylib.GetMouseWheelMove();
        if (wheelMove != 0)
        {
            _zoom = Math.Clamp(_zoom + wheelMove * 0.1f, MinZoom, MaxZoom);
        }

        // Ship movement in star map (warp travel)
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_TAB))
        {
            // Warp to nearest system
            StarSystem? nearest = FindNearestSystem(ship.Position);
            if (nearest != null && ship.Fuel > 10)
            {
                ship.Position = nearest.Position;
                ship.ConsumeFuel(10);
            }
        }
    }

    private StarSystem? FindNearestSystem(Vector2 position)
    {
        StarSystem? nearest = null;
        float minDistance = float.MaxValue;

        foreach (var system in _systems)
        {
            float distance = Vector2.Distance(position, system.Position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = system;
            }
        }

        return nearest;
    }

    public void Draw(int screenWidth, int screenHeight, Ship ship)
    {
        Vector2 center = new Vector2(screenWidth / 2, screenHeight / 2);

        // Draw star systems
        foreach (var system in _systems)
        {
            Vector2 screenPos = center + (system.Position - _cameraOffset) * _zoom;
            
            // Draw star
            Raylib.DrawCircleV(screenPos, 8 * _zoom, system.StarColor);
            
            // Draw system name
            if (_zoom > 0.7f)
            {
                Raylib.DrawText(system.Name, (int)(screenPos.X + 15), (int)(screenPos.Y - 10), 
                    (int)(16 * _zoom), Color.WHITE);
            }

            // Draw planets orbiting the star
            foreach (var planet in system.Planets)
            {
                Vector2 planetScreenPos = center + (planet.Position - _cameraOffset) * _zoom;
                Raylib.DrawCircleV(planetScreenPos, 4 * _zoom, planet.SurfaceColor);
            }
        }

        // Draw ship
        Vector2 shipScreenPos = center + (ship.Position - _cameraOffset) * _zoom;
        Raylib.DrawCircleV(shipScreenPos, 6 * _zoom, Color.WHITE);
        Raylib.DrawCircleV(shipScreenPos, 4 * _zoom, Color.BLUE);

        // Draw instructions
        Raylib.DrawText("WASD: Move | Mouse Wheel: Zoom | TAB: Warp to nearest system", 
            10, screenHeight - 50, 16, Color.YELLOW);
    }
}
