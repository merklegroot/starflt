using Raylib_cs;
using System.Numerics;

namespace StarflightGame;

public class Game
{
    private const float ManeuverTurnSpeed = 3.0f;
    private const float ManeuverThrustAcceleration = 35f;
    private const float ManeuverReverseThrustMultiplier = 0.5f;
    private const float ManeuverDragPerSecond = 1.2f;

    private readonly int _screenWidth;
    private readonly int _screenHeight;
    private GameState _currentState = GameState.CanopyView;

    private StarSystem? _currentSystem;
    private Planet? _currentPlanet;
    private readonly Ship _ship = new Ship();
    private readonly StarMapView _starMap = new StarMapView();
    private readonly ParallaxStarfield _parallax = new ParallaxStarfield();
    private readonly CanopyStarSystemView _canopySystems = new CanopyStarSystemView();
    private readonly PlanetViewRenderer _planetRenderer = new PlanetViewRenderer();
    private readonly GameMenu _menu = new GameMenu();

    private bool _justSwitchedState = false;
    private Vector2 _displayedCoordinates = Vector2.Zero;

    public bool ShouldExit { get; private set; } = false;

    public Game(int width, int height)
    {
        _screenWidth = width;
        _screenHeight = height;

        _currentSystem = _starMap.GetSystem(0);
        if (_currentSystem != null)
        {
            _ship.Position = _currentSystem.Position;
        }

        _parallax.Generate(_screenWidth, _screenHeight);
    }

    public void UnloadResources()
    {
        _planetRenderer.Unload();
    }

    public void Update()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_X))
        {
            ShouldExit = true;
            return;
        }

        float deltaTime = Raylib.GetFrameTime();

        _menu.UpdateNavigation(ref _currentState, ref _justSwitchedState);

        switch (_currentState)
        {
            case GameState.CanopyView:
                UpdateCanopyView(deltaTime);
                break;
            case GameState.Maneuver:
                UpdateManeuver(deltaTime);
                break;
            case GameState.StarMap:
                UpdateStarMap();
                break;
            case GameState.PlanetaryExploration:
                UpdatePlanetaryExploration();
                break;
            case GameState.PlanetaryEncounter:
                UpdatePlanetaryEncounter();
                break;
            case GameState.ShipStatus:
                UpdateShipStatus();
                break;
        }
    }

    private void UpdateCanopyView(float deltaTime)
    {
        _parallax.UpdateTwinkling(deltaTime);
        _canopySystems.Update(deltaTime, _starMap);
    }

    private void UpdateManeuver(float deltaTime)
    {
        if (_justSwitchedState)
        {
            _justSwitchedState = false;
            _ship.Velocity = Vector2.Zero;
            _ship.ManeuverThrustForward = false;
            _ship.ManeuverThrustReverse = false;
            _canopySystems.Update(deltaTime, _starMap);
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE))
        {
            _currentState = GameState.CanopyView;
            return;
        }

        _parallax.UpdateTwinkling(deltaTime);
        _canopySystems.Update(deltaTime, _starMap);

        _ship.ManeuverThrustForward = false;
        _ship.ManeuverThrustReverse = false;

        float turnInput = 0.0f;
        if (Raylib.IsKeyDown(KeyboardKey.KEY_A) || Raylib.IsKeyDown(KeyboardKey.KEY_LEFT))
            turnInput -= 1.0f;
        if (Raylib.IsKeyDown(KeyboardKey.KEY_D) || Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT))
            turnInput += 1.0f;

        _ship.Rotation += turnInput * ManeuverTurnSpeed * deltaTime;

        bool wantForward = Raylib.IsKeyDown(KeyboardKey.KEY_W) || Raylib.IsKeyDown(KeyboardKey.KEY_UP);
        bool wantReverse = Raylib.IsKeyDown(KeyboardKey.KEY_S) || Raylib.IsKeyDown(KeyboardKey.KEY_DOWN);

        float thrustSign = 0.0f;
        if (wantForward && !wantReverse)
            thrustSign = 1.0f;
        else if (wantReverse && !wantForward)
            thrustSign = -1.0f;

        Vector2 forward = new Vector2(MathF.Sin(_ship.Rotation), -MathF.Cos(_ship.Rotation));

        if (_ship.CanMove() && thrustSign != 0.0f)
        {
            float accelMag = ManeuverThrustAcceleration * (thrustSign > 0.0f ? 1.0f : ManeuverReverseThrustMultiplier);
            _ship.Velocity += forward * (accelMag * thrustSign * deltaTime);

            if (thrustSign > 0.0f)
                _ship.ManeuverThrustForward = true;
            else
                _ship.ManeuverThrustReverse = true;

            _ship.ConsumeFuelForMovement();
        }
        else
        {
            float dragFactor = MathF.Exp(-ManeuverDragPerSecond * deltaTime);
            _ship.Velocity *= dragFactor;
        }

        float maxSpeed = _ship.Speed;
        float speedSq = _ship.Velocity.LengthSquared();
        if (speedSq > maxSpeed * maxSpeed)
            _ship.Velocity = Vector2.Normalize(_ship.Velocity) * maxSpeed;

        Vector2 movement = _ship.Velocity * deltaTime;

        if (movement.LengthSquared() > 1e-8f)
        {
            _ship.Position += movement;
            _parallax.ApplyMovement(-movement, _screenWidth, _screenHeight, deltaTime);
            _currentSystem = _starMap.GetSystemAtPosition(_ship.Position);
        }
    }

    private void UpdateStarMap()
    {
        if (_justSwitchedState)
        {
            _justSwitchedState = false;
            _starMap.Update(_ship);
            _currentSystem = _starMap.GetSystemAtPosition(_ship.Position);
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE))
        {
            _currentState = GameState.CanopyView;
        }

        if (_menu.MenuLevel == 0 && Raylib.IsKeyPressed(KeyboardKey.KEY_ENTER) && _currentSystem != null)
        {
            _currentState = GameState.PlanetaryExploration;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_I))
        {
            _currentState = GameState.ShipStatus;
        }

        _starMap.Update(_ship);
        _currentSystem = _starMap.GetSystemAtPosition(_ship.Position);
    }

    private void UpdatePlanetaryExploration()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE) && _menu.MenuLevel == 0)
        {
            _currentState = GameState.StarMap;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_R))
        {
            _currentPlanet = null;
            _planetRenderer.ResetRotation();
        }
    }

    private void UpdatePlanetaryEncounter()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE) && _menu.MenuLevel == 0)
        {
            _currentState = GameState.CanopyView;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_R))
        {
            _currentPlanet = null;
            _planetRenderer.ResetRotation();
        }
    }

    private void UpdateShipStatus()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE))
        {
            _currentState = GameState.StarMap;
        }
    }

    public void Draw()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.BLACK);

        switch (_currentState)
        {
            case GameState.CanopyView:
                DrawCanopyView();
                _menu.DrawRightPanel(_screenWidth, _screenHeight, _ship, _currentState);
                break;
            case GameState.Maneuver:
                DrawCanopyView();
                _menu.DrawRightPanel(_screenWidth, _screenHeight, _ship, _currentState);
                break;
            case GameState.StarMap:
                _starMap.Draw(_screenWidth - LayoutConstants.RightPanelWidth, _screenHeight, _ship);
                DrawStarMapHud();
                _menu.DrawRightPanel(_screenWidth, _screenHeight, _ship, _currentState);
                break;
            case GameState.PlanetaryExploration:
                DrawPlanetaryExploration();
                DrawPlanetaryUi();
                _menu.DrawRightPanel(_screenWidth, _screenHeight, _ship, _currentState);
                break;
            case GameState.PlanetaryEncounter:
                DrawPlanetaryEncounter();
                _menu.DrawRightPanel(_screenWidth, _screenHeight, _ship, _currentState);
                break;
            case GameState.ShipStatus:
                DrawShipStatus();
                break;
        }

        Raylib.EndDrawing();
    }

    private void DrawStarMapHud()
    {
        Raylib.DrawText($"Fuel: {_ship.Fuel:F1}%", 10, 10, 20, Color.WHITE);
        Raylib.DrawText($"Credits: {_ship.Credits}", 10, 35, 20, Color.WHITE);
        Raylib.DrawText($"Press ENTER to explore planet | I for ship status | X to quit", 10, _screenHeight - 30, 16, Color.YELLOW);
    }

    private void DrawPlanetaryExploration()
    {
        if (_currentPlanet == null && _currentSystem != null)
        {
            string planetName = _planetRenderer.CreateUniquePlanetName(_currentSystem.Name);
            _currentPlanet = new Planet(planetName, Vector2.Zero, 50.0f, Color.GREEN);
        }

        if (_currentPlanet != null)
        {
            const int panelX = 200;
            const int panelY = 100;
            const int panelWidth = 400;
            const int panelHeight = 400;

            _planetRenderer.DrawExplorationPanel(_currentPlanet, panelX, panelY, panelWidth, panelHeight);
        }
    }

    private void DrawPlanetaryUi()
    {
        Raylib.DrawText($"Fuel: {_ship.Fuel:F1}%", 10, 10, 20, Color.WHITE);
        Raylib.DrawText($"Minerals: {_ship.Minerals}", 10, 35, 20, Color.WHITE);
        Raylib.DrawText($"Press ESC to return to star map | R to regenerate | X to quit", 10, _screenHeight - 30, 16, Color.YELLOW);
    }

    private void DrawPlanetaryEncounter()
    {
        int viewWidth = _screenWidth - LayoutConstants.RightPanelWidth;
        int viewHeight = _screenHeight;

        EncounterStarfield.Draw(viewWidth, viewHeight);

        if (_currentPlanet == null && _currentSystem != null)
        {
            string planetName = _planetRenderer.CreateUniquePlanetName(_currentSystem.Name);
            _currentPlanet = new Planet(planetName, Vector2.Zero, 50.0f, Color.BLUE);
        }

        if (_currentPlanet != null)
        {
            _planetRenderer.DrawEncounterFullBleed(_currentPlanet, viewWidth, viewHeight);

            Raylib.DrawText("PLANETARY ENCOUNTER", viewWidth / 2 - 150, 30, 32, Color.WHITE);

            if (_currentSystem != null)
            {
                Raylib.DrawText($"System: {_currentSystem.Name}", 40, viewHeight - 100, 18, Color.SKYBLUE);
            }
        }

        Raylib.DrawText("Press ESC to return | R to regenerate | X to quit", 40, viewHeight - 30, 18, Color.YELLOW);
    }

    private void DrawShipStatus()
    {
        int startY = 50;
        int lineHeight = 30;

        Raylib.DrawText("SHIP STATUS", _screenWidth / 2 - 100, startY, 32, Color.WHITE);

        startY += 60;
        Raylib.DrawText($"Fuel: {_ship.Fuel:F1}%", 50, startY, 24, Color.WHITE);
        startY += lineHeight;
        Raylib.DrawText($"Credits: {_ship.Credits}", 50, startY, 24, Color.WHITE);
        startY += lineHeight;
        Raylib.DrawText($"Minerals: {_ship.Minerals}", 50, startY, 24, Color.WHITE);
        startY += lineHeight;
        Raylib.DrawText($"Position: ({_ship.Position.X:F1}, {_ship.Position.Y:F1})", 50, startY, 24, Color.WHITE);

        Raylib.DrawText("Press ESC to return | X to quit", 50, _screenHeight - 50, 20, Color.YELLOW);
    }

    private void DrawCanopyView()
    {
        int viewWidth = _screenWidth - LayoutConstants.RightPanelWidth;

        _parallax.Draw(_screenWidth, _screenHeight);
        _canopySystems.Draw(_ship, _starMap, viewWidth, _screenHeight, _currentState);

        const int frameThickness = 20;
        Color frameColor = new Color(40, 40, 45, 255);

        Raylib.DrawRectangle(0, 0, viewWidth, frameThickness, frameColor);
        Raylib.DrawRectangle(0, _screenHeight - frameThickness, viewWidth, frameThickness, frameColor);
        Raylib.DrawRectangle(0, 0, frameThickness, _screenHeight, frameColor);
        Raylib.DrawRectangle(viewWidth - frameThickness, 0, frameThickness, _screenHeight, frameColor);

        Raylib.DrawText("CANOPY VIEW", 30, 30, 24, Color.WHITE);

        if (_currentState == GameState.Maneuver)
        {
            Vector2 targetPos = new Vector2(
                MathF.Round(_ship.Position.X),
                MathF.Round(_ship.Position.Y));

            if (MathF.Abs(_displayedCoordinates.X - targetPos.X) >= 1.0f)
            {
                _displayedCoordinates.X += MathF.Sign(targetPos.X - _displayedCoordinates.X);
            }
            if (MathF.Abs(_displayedCoordinates.Y - targetPos.Y) >= 1.0f)
            {
                _displayedCoordinates.Y += MathF.Sign(targetPos.Y - _displayedCoordinates.Y);
            }
        }
        else
        {
            _displayedCoordinates = new Vector2(
                MathF.Round(_ship.Position.X),
                MathF.Round(_ship.Position.Y));
        }

        string coordText = $"Coordinates: ({_displayedCoordinates.X:F0}, {_displayedCoordinates.Y:F0})";
        int coordTextWidth = Raylib.MeasureText(coordText, 18);
        int coordX = (viewWidth - coordTextWidth) / 2;
        Raylib.DrawText(coordText, coordX, 60, 18, Color.SKYBLUE);

        if (_currentState == GameState.Maneuver)
        {
            const float coastEpsilonSq = 0.01f;
            bool thrusting = _ship.ManeuverThrustForward || _ship.ManeuverThrustReverse;
            bool coasting = !thrusting && _ship.Velocity.LengthSquared() > coastEpsilonSq;

            if (thrusting)
            {
                Raylib.DrawText("Engines: thrust", 30, 90, 18, Color.GREEN);
            }
            else if (coasting)
            {
                Raylib.DrawText("Engines: coast", 30, 90, 18, new Color(230, 180, 80, 255));
            }
            else
            {
                Raylib.DrawText("Engines: idle", 30, 90, 18, new Color(120, 120, 130, 255));
            }

            Raylib.DrawText("A/D or arrows: turn | W/S or up/down: thrust / reverse | ESC: Disengage", 30, _screenHeight - 50, 16, Color.YELLOW);
        }
        else
        {
            Raylib.DrawText("Engines: OFF", 30, 90, 18, Color.RED);
            Raylib.DrawText("Use Navigator menu to access Starmap", 30, _screenHeight - 50, 16, Color.YELLOW);
        }
    }
}
