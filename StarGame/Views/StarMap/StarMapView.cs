using Raylib_cs;
using System.Numerics;
using System;
using System.IO;
using System.Text.Json;
using System.Reflection;

namespace StarflightGame.Views.StarMap;

public interface IStarMapView
{
    StarSystem? GetSystem(int index);

    List<StarSystem> GetAllSystems();

    StarSystem? GetSystemAtPosition(Vector2 position);

    void Update(IShip ship);

    void Draw(int screenWidth, int screenHeight, IShip ship);
}


/// <summary>
/// Star map data and presentation: loads systems from embedded JSON, pan/zoom camera, warp-to-nearest (Tab),
/// and 2D map drawing including the ship. Used by the canopy view for world positions and by map mode for interaction.
/// </summary>
public class StarMapView : IStarMapView
{
    private List<StarSystem> _systems = LoadStarSystems();

    private StarMapViewState _state = new StarMapViewState();

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

    public void Update(IShip ship)
    {
        // Camera movement
        Vector2 movement = Vector2.Zero;
        float speed = 5.0f / _state.Zoom;

        if (Raylib.IsKeyDown(KeyboardKey.KEY_W) || Raylib.IsKeyDown(KeyboardKey.KEY_UP))
            movement.Y -= speed;
        if (Raylib.IsKeyDown(KeyboardKey.KEY_S) || Raylib.IsKeyDown(KeyboardKey.KEY_DOWN))
            movement.Y += speed;
        if (Raylib.IsKeyDown(KeyboardKey.KEY_A) || Raylib.IsKeyDown(KeyboardKey.KEY_LEFT))
            movement.X -= speed;
        if (Raylib.IsKeyDown(KeyboardKey.KEY_D) || Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT))
            movement.X += speed;

        _state = _state with { CameraOffset = _state.CameraOffset + movement };

        // Zoom
        float wheelMove = Raylib.GetMouseWheelMove();
        if (wheelMove != 0)
        {
            _state = _state with
            {
                Zoom = Math.Clamp(
                    _state.Zoom + wheelMove * 0.1f,
                    StarMapViewState.MinZoom,
                    StarMapViewState.MaxZoom)
            };
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

    public void Draw(int screenWidth, int screenHeight, IShip ship)
    {
        Vector2 center = new Vector2(screenWidth / 2, screenHeight / 2);

        // Draw star systems
        foreach (var system in _systems)
        {
            Vector2 screenPos = center + (system.Position - _state.CameraOffset) * _state.Zoom;

            // Draw star
            Raylib.DrawCircleV(screenPos, 8 * _state.Zoom, system.StarColor);

            // Draw system name
            if (_state.Zoom > 0.7f)
            {
                Raylib.DrawText(system.Name, (int)(screenPos.X + 15), (int)(screenPos.Y - 10),
                    (int)(16 * _state.Zoom), Color.WHITE);
            }

        }

        // Draw ship
        Vector2 shipScreenPos = center + (ship.Position - _state.CameraOffset) * _state.Zoom;
        Raylib.DrawCircleV(shipScreenPos, 6 * _state.Zoom, Color.WHITE);
        Raylib.DrawCircleV(shipScreenPos, 4 * _state.Zoom, Color.BLUE);

        // Draw instructions
        Raylib.DrawText("WASD: Move | Mouse Wheel: Zoom | TAB: Warp to nearest system",
            10, screenHeight - 50, 16, Color.YELLOW);
    }
}
