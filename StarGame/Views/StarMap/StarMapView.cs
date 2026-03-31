using Raylib_cs;
using StarflightGame;
using System.Numerics;

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
    private readonly List<StarSystem> _systems;

    private StarMapViewState _state = new StarMapViewState();

    public StarMapView(IResourceLoader resourceLoader)
    {
        _systems = resourceLoader.LoadStarSystems();
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

            // Draw system name (visible at all zoom levels; floor font size for readability when zoomed out)
            int nameFontSize = Math.Max(10, (int)(16 * _state.Zoom));
            UiText.DrawText(system.Name, (int)(screenPos.X + 15), (int)(screenPos.Y - 10),
                nameFontSize, Color.WHITE);

        }

        // Draw ship
        Vector2 shipScreenPos = center + (ship.Position - _state.CameraOffset) * _state.Zoom;
        Raylib.DrawCircleV(shipScreenPos, 6 * _state.Zoom, Color.WHITE);
        Raylib.DrawCircleV(shipScreenPos, 4 * _state.Zoom, Color.BLUE);

        // Draw instructions
        UiText.DrawText("WASD: Move | Mouse Wheel: Zoom | TAB: Warp to nearest system",
            10, screenHeight - 50, 16, Color.YELLOW);
    }
}
