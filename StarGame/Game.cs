using Raylib_cs;
using System.Numerics;
using StarflightGame.Combat;
using StarflightGame.Constants;
using StarflightGame.Mining;
using StarflightGame.Views;
using StarflightGame.Views.StarMap;

namespace StarflightGame;

public interface IGame
{
    void Run();
    void Update();
    void Draw();
    void UnloadResources();
    bool ShouldExit { get; }
}

public class Game : IGame
{
    private const float TurnSpeed = 3.0f;
    private const float ManeuverThrustAcceleration = 35f;
    private const float ManeuverReverseThrustMultiplier = 0.5f;
    private const float ManeuverDragPerSecond = 0.45f;
    private const float ManeuverVelocityStopEpsilonSq = 0.01f;
    private const float ManeuverParallaxMatchMultiplier = 20f;

    private const float StarSystemThrustAcceleration = 170f;
    private const float StarSystemSpeedMultiplier = 30.0f;

    /// <summary>Max distance in screen pixels from view center to a star's drawn position for SPACE / hint (matches canopy placement + star radius ~20).</summary>
    private const float CanopyStarEnterRadiusPixels = 28f;

    /// <summary>Exit star system interior when ship offset from the star exceeds this fraction of the smaller main-view dimension (same units as orbit layout).</summary>
    private const float StarSystemInteriorExitBoundaryFraction = 0.42f;
    private readonly int _screenWidth;
    private readonly int _screenHeight;

    private int MainViewWidth => LayoutUtility.MainViewWidth(_screenWidth);

    private GameState _currentState = GameState.CanopyView;

    private StarSystem? _currentSystem;
    private Planet? _currentPlanet;
    private readonly IShip _ship;
    private readonly IStarMapView _starMap;
    private readonly IParallaxStarfield _parallax;
    private readonly ICanopyStarSystemView _canopySystems;
    private readonly IStarSystemInteriorView _starSystemInteriorView;
    private readonly IPlanetView _planetView;
    private readonly ICombatView _combatView;
    private readonly IRightPanel _rightPanel;
    private readonly IResourceLoader _resourceLoader;
    private readonly IPlanetMiningRigStore _planetMiningRigStore;
    private readonly IReadOnlyList<MineralTradeEntry> _mineralTradeList;

    private bool _justSwitchedState = false;
    private string _miningRigStatusMessage = "";
    private float _miningRigStatusTimer = 0f;
    private Vector2 _displayedCoordinates = Vector2.Zero;
    private Vector2 _maneuverParallaxBoost = Vector2.Zero;
    private Vector2 _starSystemShipPosition = Vector2.Zero;
    private Vector2 _starSystemVelocity = Vector2.Zero;
    private GameState _previousState = GameState.CanopyView;

    private GameState _planetaryEncounterReturnState = GameState.CanopyView;
    private GameState _manifestReturnState = GameState.CanopyView;
    private GameState _combatReturnState = GameState.CanopyView;

    public bool ShouldExit { get; private set; } = false;

    public Game(
        IShip ship,
        IRightPanel rightPanel,
        IStarMapView starMap,
        IParallaxStarfield parallax,
        ICanopyStarSystemView canopySystems,
        IStarSystemInteriorView starSystemInteriorView,
        IPlanetView planetView,
        ICombatView combatView,
        IResourceLoader resourceLoader,
        IPlanetMiningRigStore planetMiningRigStore)
    {
        _ship = ship;
        _rightPanel = rightPanel;
        _starMap = starMap;
        _parallax = parallax;
        _canopySystems = canopySystems;
        _starSystemInteriorView = starSystemInteriorView;
        _planetView = planetView;
        _combatView = combatView;
        _resourceLoader = resourceLoader;
        _planetMiningRigStore = planetMiningRigStore;
        _mineralTradeList = resourceLoader.LoadMineralTradeList();
        _screenWidth = GameConstants.ScreenWidth;
        _screenHeight = GameConstants.ScreenHeight;

        _currentSystem = _starMap.GetSystem(0);
        if (_currentSystem != null)
        {
            _ship.Position = _currentSystem.Position;
        }

        _parallax.Generate(_screenWidth, _screenHeight);
    }

    public void Run()
    {
        Raylib.InitWindow(GameConstants.ScreenWidth, GameConstants.ScreenHeight, "Starflight");
        Raylib.SetTargetFPS(60);
        InputManager.Initialize();
        UiText.Load();
        ShipRenderer.Load();

        while (!ShouldExit && !WindowShouldClose())
        {
            Update();
            Draw();
        }

        UnloadResources();
        Raylib.CloseWindow();
    }

    public void UnloadResources()
    {
        UiText.Unload();
        ShipRenderer.Unload();
        _planetView.Unload();
    }

    public void Update()
    {
        InputManager.Update();

        if (InputManager.IsQuitPressed())
        {
            ShouldExit = true;
            return;
        }

        float deltaTime = Raylib.GetFrameTime();

        UpdateMiningProduction(deltaTime);
        UpdateMiningRigStatusMessage(deltaTime);

        _rightPanel.UpdateNavigation(ref _currentState, ref _justSwitchedState);

        if (_currentState == GameState.PlanetaryEncounter
            && _previousState != GameState.PlanetaryEncounter
            && _previousState != GameState.StarSystemView)
        {
            _planetaryEncounterReturnState = GameState.CanopyView;

            if (TryGetJupiterPlanet(out LoadedPlanet jupiter))
            {
                _currentPlanet = new Planet(
                    jupiter.Name,
                    Vector2.Zero,
                    50.0f,
                    jupiter.SurfaceColor,
                    jupiter.RadiusKm,
                    jupiter.Rings,
                    jupiter.Composition);
                _planetView.ResetRotation();
            }
        }

        if (_currentState == GameState.StarSystemView && _previousState != GameState.StarSystemView)
        {
            _starSystemInteriorView.NotifyStarSystemViewEntered(_currentSystem);
        }

        switch (_currentState)
        {
            case GameState.CanopyView:
                UpdateCanopyView(deltaTime);
                break;
            case GameState.StarMap:
                UpdateStarMap();
                break;
            case GameState.StarSystemView:
                UpdateStarSystemView(deltaTime);
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
            case GameState.MineralCatalog:
                UpdateMineralCatalog();
                break;
            case GameState.ShipManifest:
                UpdateShipManifest();
                break;
            case GameState.Combat:
                UpdateCombat(deltaTime);
                break;
        }

        if (_rightPanel.MenuLevel == 0
            && InputManager.IsCombatPressed()
            && (_currentState == GameState.CanopyView || _currentState == GameState.StarSystemView))
        {
            _combatReturnState = _currentState;
            _currentState = GameState.Combat;
            _justSwitchedState = true;
        }

        if (_currentState == GameState.CanopyView
            && _rightPanel.MenuLevel == 0
            && InputManager.IsEnterStarSystemPressed())
        {
            StarSystem? nearby = GetSystemNearCanopyCrosshair();
            if (nearby != null)
            {
                _currentSystem = nearby;
                _ship.Position = nearby.Position;
                _maneuverParallaxBoost = Vector2.Zero;
                _currentState = GameState.StarSystemView;
                _justSwitchedState = true;
                _starSystemInteriorView.NotifyStarSystemViewEntered(_currentSystem);
            }
        }

        if (_currentState == GameState.CanopyView
            && _rightPanel.MenuLevel == 0
            && Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
        {
            Vector2 mouse = Raylib.GetMousePosition();
            StarSystem? clicked = GetSystemNearCanopyScreenPosition(mouse);
            if (clicked != null)
            {
                _currentSystem = clicked;
                _ship.Position = clicked.Position;
                _maneuverParallaxBoost = Vector2.Zero;
                _currentState = GameState.StarSystemView;
                _justSwitchedState = true;
                _starSystemInteriorView.NotifyStarSystemViewEntered(_currentSystem);
            }
        }

        if (_previousState == GameState.StarSystemView
            && _currentState != GameState.StarSystemView
            && _currentState != GameState.Combat)
        {
            _ship.Velocity = Vector2.Zero;
            _ship.ManeuverThrustForward = false;
            _ship.ManeuverThrustReverse = false;
        }

        if (_previousState == GameState.Combat && _currentState != GameState.Combat)
        {
            _ship.Velocity = Vector2.Zero;
            _ship.ManeuverThrustForward = false;
            _ship.ManeuverThrustReverse = false;
        }

        if (_rightPanel.MenuLevel == 0
            && InputManager.IsManifestPressed()
            && _currentState != GameState.ShipManifest
            && _currentState != GameState.PlanetaryEncounter)
        {
            _currentState = GameState.ShipManifest;
            _justSwitchedState = true;
        }

        if (_rightPanel.MenuLevel == 0
            && InputManager.IsRefuelPressed()
            && _currentState != GameState.PlanetaryExploration
            && _currentState != GameState.PlanetaryEncounter)
        {
            _ship.RefuelToFull();
        }

        if (_rightPanel.MenuLevel == 0
            && InputManager.IsDebugHealPressed()
            && _currentState != GameState.PlanetaryEncounter)
        {
            _ship.RestoreShipForDebug();
            if (_currentState == GameState.Combat)
            {
                _combatView.RestoreForDebug();
            }
        }

        if (_currentState == GameState.ShipManifest && _previousState != GameState.ShipManifest)
        {
            _manifestReturnState = _previousState;
        }

        _previousState = _currentState;
    }

    /// <summary>
    /// Same screen position and wobble as <see cref="CanopyStarSystemView.Draw"/> (not raw world distance).
    /// </summary>
    private StarSystem? GetSystemNearCanopyCrosshair()
    {
        int viewWidth = MainViewWidth;
        return _canopySystems.FindSystemNearCrosshair(
            _ship,
            _starMap,
            viewWidth,
            _screenHeight,
            _maneuverParallaxBoost,
            CanopyStarEnterRadiusPixels);
    }

    private StarSystem? GetSystemNearCanopyScreenPosition(Vector2 screenPosition)
    {
        int viewWidth = MainViewWidth;
        if (screenPosition.X < 0 || screenPosition.X >= viewWidth || screenPosition.Y < 0 || screenPosition.Y >= _screenHeight)
        {
            return null;
        }

        StarSystem? best = null;
        float bestDistSq = float.MaxValue;

        foreach (var system in _starMap.GetAllSystems())
        {
            _canopySystems.GetStarScreenPosition(
                _ship,
                system,
                viewWidth,
                _screenHeight,
                _maneuverParallaxBoost,
                out int sx,
                out int sy);

            float dx = sx - screenPosition.X;
            float dy = sy - screenPosition.Y;
            float dSq = dx * dx + dy * dy;
            if (dSq < bestDistSq)
            {
                bestDistSq = dSq;
                best = system;
            }
        }

        float maxSq = CanopyStarEnterRadiusPixels * CanopyStarEnterRadiusPixels;
        if (best != null && bestDistSq <= maxSq)
        {
            return best;
        }

        return null;
    }

    private void UpdateCanopyView(float deltaTime)
    {
        if (_justSwitchedState)
        {
            _justSwitchedState = false;
            _ship.Velocity = Vector2.Zero;
            _ship.ManeuverThrustForward = false;
            _ship.ManeuverThrustReverse = false;
            _parallax.UpdateTwinkling(deltaTime);
            _canopySystems.Update(deltaTime, _starMap);
            _currentSystem = GetSystemNearCanopyCrosshair();
            return;
        }

        _parallax.UpdateTwinkling(deltaTime);
        _canopySystems.Update(deltaTime, _starMap);

        _ship.ManeuverThrustForward = false;
        _ship.ManeuverThrustReverse = false;

        float turnInput = InputManager.GetTurnAxis();

        _ship.Rotation += turnInput * TurnSpeed * deltaTime;

        InputManager.GetThrustHeld(out bool wantForward, out bool wantReverse);

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

            if (_ship.Velocity.LengthSquared() < ManeuverVelocityStopEpsilonSq)
                _ship.Velocity = Vector2.Zero;
        }

        float maxSpeed = _ship.Speed;
        float speedSq = _ship.Velocity.LengthSquared();
        if (speedSq > maxSpeed * maxSpeed)
            _ship.Velocity = Vector2.Normalize(_ship.Velocity) * maxSpeed;

        Vector2 movement = _ship.Velocity * deltaTime;

        _ship.Position += movement;
        _parallax.ApplyMovement(-movement, _screenWidth, _screenHeight, deltaTime);
        _maneuverParallaxBoost += -movement * (ManeuverParallaxMatchMultiplier - 1f);
        _currentSystem = GetSystemNearCanopyCrosshair();
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

        if (InputManager.IsBackPressed())
        {
            _currentState = GameState.CanopyView;
        }

        if (_rightPanel.MenuLevel == 0 && Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
        {
            Vector2 mouse = Raylib.GetMousePosition();
            if (mouse.X >= 0 && mouse.X < MainViewWidth && mouse.Y >= 0 && mouse.Y < _screenHeight)
            {
                StarSystem? clicked = _starMap.GetSystemAtScreenPosition(mouse, MainViewWidth, _screenHeight);
                if (clicked != null)
                {
                    _currentSystem = clicked;
                    _ship.Position = clicked.Position;
                    _currentState = GameState.StarSystemView;
                    _justSwitchedState = true;
                    _starSystemInteriorView.NotifyStarSystemViewEntered(_currentSystem);
                    return;
                }
            }
        }

        if (_rightPanel.MenuLevel == 0 && InputManager.IsConfirmPressed() && _currentSystem != null)
        {
            _currentState = GameState.PlanetaryExploration;
        }

        if (InputManager.IsShipStatusPressed())
        {
            _currentState = GameState.ShipStatus;
        }

        _starMap.Update(_ship);
        _currentSystem = _starMap.GetSystemAtPosition(_ship.Position);
    }

    private void UpdateStarSystemView(float deltaTime)
    {
        if (_justSwitchedState)
        {
            _justSwitchedState = false;
            // Must match canopy math: screen offset = system.Position - ship.Position + maneuverParallaxBoost.
            // Raw ship.Position alone can still be near Sol while parallax centers another star — then GetSystemAtPosition returned Sol first.
            Vector2 worldForSystemLookup = _ship.Position - _maneuverParallaxBoost;
            StarSystem? resolved = _starMap.GetSystemAtPosition(worldForSystemLookup);
            if (resolved != null)
            {
                _currentSystem = resolved;
            }

            _starSystemVelocity = Vector2.Zero;
            _ship.Velocity = Vector2.Zero;
            _ship.ManeuverThrustForward = false;
            _ship.ManeuverThrustReverse = false;
            _starSystemShipPosition = Vector2.Zero;
            return;
        }

        if (InputManager.IsBackPressed() && _rightPanel.MenuLevel == 0)
        {
            _currentState = GameState.CanopyView;
            return;
        }

        TryCycleStarSystemInteriorView();

        _starSystemInteriorView.UpdateStarSystemUiInput();

        _ship.ManeuverThrustForward = false;
        _ship.ManeuverThrustReverse = false;

        float turnInput = InputManager.GetTurnAxis();

        _ship.Rotation += turnInput * TurnSpeed * deltaTime;

        InputManager.GetThrustHeld(out bool wantForward, out bool wantReverse);

        float thrustSign = 0.0f;
        if (wantForward && !wantReverse)
            thrustSign = 1.0f;
        else if (wantReverse && !wantForward)
            thrustSign = -1.0f;

        Vector2 forward = new Vector2(MathF.Sin(_ship.Rotation), -MathF.Cos(_ship.Rotation));

        if (_ship.CanMove() && thrustSign != 0.0f)
        {
            float accelMag = StarSystemThrustAcceleration * (thrustSign > 0.0f ? 1.0f : ManeuverReverseThrustMultiplier);
            _starSystemVelocity += forward * (accelMag * thrustSign * deltaTime);

            if (thrustSign > 0.0f)
                _ship.ManeuverThrustForward = true;
            else
                _ship.ManeuverThrustReverse = true;

            _ship.ConsumeFuelForMovement();
        }
        else
        {
            float dragFactor = MathF.Exp(-ManeuverDragPerSecond * deltaTime);
            _starSystemVelocity *= dragFactor;

            if (_starSystemVelocity.LengthSquared() < ManeuverVelocityStopEpsilonSq)
                _starSystemVelocity = Vector2.Zero;
        }

        float maxSpeed = _ship.Speed * StarSystemSpeedMultiplier;
        float speedSq = _starSystemVelocity.LengthSquared();
        if (speedSq > maxSpeed * maxSpeed)
            _starSystemVelocity = Vector2.Normalize(_starSystemVelocity) * maxSpeed;

        _starSystemShipPosition += _starSystemVelocity * deltaTime;
        _ship.Velocity = _starSystemVelocity;

        int mainViewW = MainViewWidth;

        if (_rightPanel.MenuLevel == 0
            && _starSystemInteriorView.TryGetPlanetOverlappingShip(
                _currentSystem,
                _starSystemShipPosition,
                mainViewW,
                _screenHeight,
                out LoadedPlanet loadedPlanet))
        {
            _currentPlanet = new Planet(
                loadedPlanet.Name,
                Vector2.Zero,
                50.0f,
                loadedPlanet.SurfaceColor,
                loadedPlanet.RadiusKm,
                loadedPlanet.Rings,
                loadedPlanet.Composition);
            _planetView.ResetRotation();
            _planetaryEncounterReturnState = GameState.StarSystemView;
            _currentState = GameState.PlanetaryEncounter;
            _justSwitchedState = true;
            return;
        }

        float exitRadius = MathF.Min(mainViewW, _screenHeight) * StarSystemInteriorExitBoundaryFraction;
        if (_starSystemShipPosition.LengthSquared() >= exitRadius * exitRadius)
        {
            _currentState = GameState.CanopyView;
            _starSystemVelocity = Vector2.Zero;
            _starSystemShipPosition = Vector2.Zero;
            _ship.Velocity = Vector2.Zero;
            _ship.ManeuverThrustForward = false;
            _ship.ManeuverThrustReverse = false;
        }
    }

    private void UpdatePlanetaryExploration()
    {
        if (InputManager.IsBackPressed() && _rightPanel.MenuLevel == 0)
        {
            _currentState = GameState.StarMap;
        }

        if (InputManager.IsRefuelPressed())
        {
            _currentPlanet = null;
            _planetView.ResetRotation();
        }
    }

    private void UpdatePlanetaryEncounter()
    {
        if (InputManager.IsBackPressed() && _rightPanel.MenuLevel == 0)
        {
            _currentState = _planetaryEncounterReturnState;
        }

        if (InputManager.IsRefuelPressed() && _rightPanel.MenuLevel == 0)
        {
            if (_currentSystem != null)
            {
                string planetName = _planetView.CreateUniquePlanetName(_currentSystem.Name);
                _currentPlanet = new Planet(planetName, Vector2.Zero, 50.0f, Color.BLUE);
            }
            else
            {
                _currentPlanet = null;
            }

            _planetView.ResetRotation();
        }

        if (_rightPanel.MenuLevel == 0 && TryGetPlanetaryEncounterContext(out string systemId, out Planet planet))
        {
            if (InputManager.IsManifestPressed())
            {
                TryDeployMiningRig(systemId, planet);
            }

            if (InputManager.IsDebugHealPressed())
            {
                TryHarvestPlanetMinerals(systemId, planet);
            }
        }
    }

    private void UpdateMiningProduction(float deltaTime) =>
        _planetMiningRigStore.UpdateProduction(deltaTime);

    private void UpdateMiningRigStatusMessage(float deltaTime)
    {
        if (_miningRigStatusTimer <= 0f)
        {
            return;
        }

        _miningRigStatusTimer -= deltaTime;
        if (_miningRigStatusTimer <= 0f)
        {
            _miningRigStatusMessage = "";
        }
    }

    private void TryDeployMiningRig(string systemId, Planet planet)
    {
        if (!_planetMiningRigStore.TryAddRig(systemId, planet.Name, out string failureReason))
        {
            SetMiningRigStatusMessage(failureReason);
            return;
        }

        int count = _planetMiningRigStore.GetRigCount(systemId, planet.Name);
        SetMiningRigStatusMessage($"Mining rig deployed ({count} on {planet.Name}).");
    }

    private void TryHarvestPlanetMinerals(string systemId, Planet planet)
    {
        if (!_planetMiningRigStore.TryHarvest(systemId, planet.Name, out int harvested, out string failureReason))
        {
            SetMiningRigStatusMessage(failureReason);
            return;
        }

        _ship.AddMinerals(harvested);
        SetMiningRigStatusMessage($"Harvested {harvested} minerals from {planet.Name}.");
    }

    private void SetMiningRigStatusMessage(string message)
    {
        _miningRigStatusMessage = message;
        _miningRigStatusTimer = 3.5f;
    }

    private bool TryGetPlanetaryEncounterContext(out string systemId, out Planet planet)
    {
        systemId = "";
        planet = null!;
        if (_currentPlanet == null || _currentSystem == null)
        {
            return false;
        }

        systemId = _currentSystem.Id.ToLowerInvariant();
        planet = _currentPlanet;
        return true;
    }

    private void UpdateShipStatus()
    {
        if (InputManager.IsBackPressed())
        {
            _currentState = GameState.StarMap;
        }
    }

    private void UpdateMineralCatalog()
    {
        if (InputManager.IsBackPressed())
        {
            _currentState = GameState.CanopyView;
        }
    }

    private void UpdateShipManifest()
    {
        if (InputManager.IsBackPressed())
        {
            _currentState = _manifestReturnState;
        }
    }

    private void UpdateCombat(float deltaTime)
    {
        int viewWidth = MainViewWidth;

        if (_justSwitchedState)
        {
            _justSwitchedState = false;
            _combatView.BeginSession(viewWidth, _screenHeight, _ship);
            return;
        }

        if (InputManager.IsBackPressed() && _rightPanel.MenuLevel == 0)
        {
            _currentState = _combatReturnState;
            _justSwitchedState = true;
            return;
        }

        _combatView.Update(deltaTime, viewWidth, _screenHeight, _ship);
    }

    public void Draw()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.BLACK);

        switch (_currentState)
        {
            case GameState.CanopyView:
                DrawCanopyView();
                _rightPanel.Draw(_screenWidth, _screenHeight, _ship, _currentState);
                break;
            case GameState.StarMap:
                _starMap.Draw(MainViewWidth, _screenHeight, _ship);
                DrawStarMapHud();
                _rightPanel.Draw(_screenWidth, _screenHeight, _ship, _currentState);
                break;
            case GameState.StarSystemView:
                DrawStarSystemView();
                _rightPanel.Draw(
                    _screenWidth,
                    _screenHeight,
                    _ship,
                    _currentState,
                    _starSystemShipPosition,
                    _currentSystem,
                    MainViewWidth,
                    _screenHeight);
                break;
            case GameState.PlanetaryExploration:
                DrawPlanetaryExploration();
                DrawPlanetaryUi();
                _rightPanel.Draw(_screenWidth, _screenHeight, _ship, _currentState);
                break;
            case GameState.PlanetaryEncounter:
                DrawPlanetaryEncounter();
                _rightPanel.Draw(_screenWidth, _screenHeight, _ship, _currentState);
                break;
            case GameState.ShipStatus:
                DrawShipStatus();
                break;
            case GameState.MineralCatalog:
                DrawMineralCatalog();
                _rightPanel.Draw(_screenWidth, _screenHeight, _ship, _currentState);
                break;
            case GameState.ShipManifest:
                DrawShipManifest();
                _rightPanel.Draw(_screenWidth, _screenHeight, _ship, _currentState);
                break;
            case GameState.Combat:
                DrawCombat();
                _rightPanel.Draw(
                    _screenWidth,
                    _screenHeight,
                    _ship,
                    _currentState,
                    _combatView.PlayerPosition);
                break;
        }

        Raylib.EndDrawing();
    }

    private void DrawCombat()
    {
        _combatView.Draw(MainViewWidth, _screenHeight, _ship);
    }

    private void DrawStarSystemView()
    {
        int viewWidth = MainViewWidth;

        _starSystemInteriorView.Draw(_currentSystem, viewWidth, _screenHeight, _starSystemShipPosition, _ship);

        const int frameThickness = 20;
        Color frameColor = new Color(40, 40, 45, 255);

        Raylib.DrawRectangle(0, 0, viewWidth, frameThickness, frameColor);
        Raylib.DrawRectangle(0, _screenHeight - frameThickness, viewWidth, frameThickness, frameColor);
        Raylib.DrawRectangle(0, 0, frameThickness, _screenHeight, frameColor);
        Raylib.DrawRectangle(viewWidth - frameThickness, 0, frameThickness, _screenHeight, frameColor);

        UiText.DrawText(
            "L2/R2 systems | P list | X combat | Stick fly | A enter star | B canopy | fly far: exit",
            24,
            _screenHeight - frameThickness + 4,
            14,
            new Color(220, 210, 140, 255));
    }

    private void TryCycleStarSystemInteriorView()
    {
        int delta = 0;
        if (InputManager.IsPreviousSystemPressed())
        {
            delta = -1;
        }
        else if (InputManager.IsNextSystemPressed())
        {
            delta = 1;
        }

        if (delta == 0)
        {
            return;
        }

        List<StarSystem> systems = _starMap.GetAllSystems();
        if (systems.Count == 0)
        {
            return;
        }

        int index = FindStarSystemIndex(systems, _currentSystem);
        index = (index + delta + systems.Count) % systems.Count;
        ApplyStarSystemInteriorSelection(systems[index]);
    }

    private static int FindStarSystemIndex(List<StarSystem> systems, StarSystem? current)
    {
        if (current == null)
        {
            return 0;
        }

        for (int i = 0; i < systems.Count; i++)
        {
            if (systems[i].Id == current.Id)
            {
                return i;
            }
        }

        return 0;
    }

    private void ApplyStarSystemInteriorSelection(StarSystem system)
    {
        _currentSystem = system;
        _starSystemShipPosition = Vector2.Zero;
        _starSystemVelocity = Vector2.Zero;
        _ship.Velocity = Vector2.Zero;
        _starSystemInteriorView.NotifyStarSystemViewEntered(_currentSystem);
    }

    private bool TryGetJupiterPlanet(out LoadedPlanet jupiter)
    {
        jupiter = default;
        IReadOnlyDictionary<string, LoadedPlanet[]> catalog = _resourceLoader.LoadPlanetsByStarSystem();

        if (!catalog.TryGetValue("sol", out LoadedPlanet[]? planets))
        {
            return false;
        }

        for (int i = 0; i < planets.Length; i++)
        {
            if (string.Equals(planets[i].Name, "Jupiter", StringComparison.OrdinalIgnoreCase))
            {
                jupiter = planets[i];
                return true;
            }
        }

        return false;
    }

    private static string FormatCompositionLabel(PlanetComposition composition)
    {
        return composition switch
        {
            PlanetComposition.Terrestrial => "Terrestrial",
            PlanetComposition.GasGiant => "Gas giant",
            PlanetComposition.IceGiant => "Ice giant",
            PlanetComposition.Molten => "Molten",
            _ => composition.ToString()
        };
    }

    private void DrawStarMapHud()
    {
        UiText.DrawText($"Fuel: {_ship.FuelQuantity:F1} / {_ship.FuelCapacity}", 10, 10, 20, Color.WHITE);
        UiText.DrawText($"Credits: {_ship.Credits}", 10, 35, 20, Color.WHITE);
        UiText.DrawText($"Press ENTER to explore planet | I for ship status | X to quit", 10, _screenHeight - 30, 16, Color.YELLOW);
    }

    private void DrawPlanetaryExploration()
    {
        if (_currentPlanet == null && _currentSystem != null)
        {
            string planetName = _planetView.CreateUniquePlanetName(_currentSystem.Name);
            _currentPlanet = new Planet(planetName, Vector2.Zero, 50.0f, Color.GREEN);
        }

        if (_currentPlanet != null)
        {
            const int panelX = 200;
            const int panelY = 100;
            const int panelWidth = 400;
            const int panelHeight = 400;

            _planetView.DrawExplorationPanel(_currentPlanet, panelX, panelY, panelWidth, panelHeight);
        }
    }

    private void DrawPlanetaryUi()
    {
        UiText.DrawText($"Fuel: {_ship.FuelQuantity:F1} / {_ship.FuelCapacity}", 10, 10, 20, Color.WHITE);
        UiText.DrawText($"Minerals: {_ship.Minerals}", 10, 35, 20, Color.WHITE);
        UiText.DrawText($"Press ESC to return to star map | R to regenerate | X to quit", 10, _screenHeight - 30, 16, Color.YELLOW);
    }

    private void DrawPlanetaryEncounter()
    {
        int viewWidth = MainViewWidth;
        int viewHeight = _screenHeight;

        EncounterStarfield.Draw(viewWidth, viewHeight);

        if (_currentPlanet == null && _currentSystem != null)
        {
            if (TryGetJupiterPlanet(out LoadedPlanet jupiter))
            {
                _currentPlanet = new Planet(
                    jupiter.Name,
                    Vector2.Zero,
                    50.0f,
                    jupiter.SurfaceColor,
                    jupiter.RadiusKm,
                    jupiter.Rings,
                    jupiter.Composition);
            }
            else
            {
                string planetName = _planetView.CreateUniquePlanetName(_currentSystem.Name);
                _currentPlanet = new Planet(planetName, Vector2.Zero, 50.0f, Color.BLUE);
            }
        }

        if (_currentPlanet != null)
        {
            _planetView.DrawEncounterFullBleed(_currentPlanet, viewWidth, viewHeight);

            const int titleSize = 22;
            const int planetNameSize = 34;
            float titleY = 22;
            UiText.DrawTextCenteredAtXClamped(
                "PLANETARY ENCOUNTER",
                viewWidth * 0.5f,
                titleY,
                titleSize,
                new Color(200, 205, 220, 255),
                12f,
                viewWidth - 12f);

            float planetLineY = titleY + titleSize + 10;
            UiText.DrawTextCenteredAtXOutlined(
                _currentPlanet.Name,
                viewWidth * 0.5f,
                planetLineY,
                planetNameSize,
                Color.WHITE,
                new Color(0, 0, 0, 200));

            const int infoLine = 20;
            int infoY = viewHeight - 138;
            if (_currentSystem != null)
            {
                UiText.DrawText($"System: {_currentSystem.Name}", 40, infoY, 18, Color.SKYBLUE);
                infoY += infoLine;
            }

            UiText.DrawText(
                $"Composition: {FormatCompositionLabel(_currentPlanet.Composition)}",
                40,
                infoY,
                18,
                new Color(190, 200, 225, 255));
            infoY += infoLine;

            if (_currentPlanet.RadiusKm > 0f)
            {
                UiText.DrawText($"Radius: {_currentPlanet.RadiusKm:N0} km", 40, infoY, 18, new Color(180, 210, 235, 255));
                infoY += infoLine;
            }

            if (_currentPlanet.Rings.HasValue && _currentPlanet.Rings.Value.IsValid)
            {
                PlanetRingData r = _currentPlanet.Rings.Value;
                UiText.DrawText(
                    $"Rings: {r.InnerRadiusKm:N0} – {r.OuterRadiusKm:N0} km  |  thickness ~{r.ThicknessKm:F2} km",
                    40,
                    infoY,
                    16,
                    new Color(220, 200, 160, 255));
                infoY += infoLine;

                UiText.DrawText(
                    $"Opacity: {r.Opacity:F2}  |  texture: {r.ParticleTexture}  |  radial division: {(r.HasGaps ? "yes" : "no")}",
                    40,
                    infoY,
                    16,
                    new Color(160, 175, 195, 255));
            }

            if (TryGetPlanetaryEncounterContext(out string rigSystemId, out Planet rigPlanet))
            {
                int rigCount = _planetMiningRigStore.GetRigCount(rigSystemId, rigPlanet.Name);
                int stored = _planetMiningRigStore.GetStoredMinerals(rigSystemId, rigPlanet.Name);
                UiText.DrawText($"Mining rigs: {rigCount}", 40, infoY, 18, new Color(255, 200, 90, 255));
                infoY += infoLine;
                UiText.DrawText($"Stored minerals: {stored}", 40, infoY, 18, new Color(255, 200, 90, 255));
                infoY += infoLine;

                UiText.DrawText("M deploy rig | H harvest minerals", 40, infoY, 16, new Color(210, 220, 140, 255));
                infoY += infoLine;
            }

            if (!string.IsNullOrEmpty(_miningRigStatusMessage))
            {
                UiText.DrawTextCenteredAtXClamped(
                    _miningRigStatusMessage,
                    viewWidth * 0.5f,
                    viewHeight * 0.5f + 80f,
                    20,
                    new Color(255, 220, 120, 255),
                    40f,
                    viewWidth - 40f);
            }
        }

        UiText.DrawText("Press ESC to return | R to regenerate | X to quit", 40, viewHeight - 30, 18, Color.YELLOW);
    }

    private void DrawShipStatus()
    {
        int startY = 50;
        int lineHeight = 30;

        UiText.DrawText("SHIP STATUS", _screenWidth / 2 - 100, startY, 32, Color.WHITE);

        startY += 60;
        UiText.DrawText($"Fuel: {_ship.FuelQuantity:F1} / {_ship.FuelCapacity}", 50, startY, 24, Color.WHITE);
        startY += lineHeight;
        UiText.DrawText($"Credits: {_ship.Credits}", 50, startY, 24, Color.WHITE);
        startY += lineHeight;
        UiText.DrawText($"Minerals: {_ship.Minerals}", 50, startY, 24, Color.WHITE);
        startY += lineHeight;
        UiText.DrawText($"Position: ({_ship.Position.X:F1}, {_ship.Position.Y:F1})", 50, startY, 24, Color.WHITE);

        UiText.DrawText("Press ESC to return | X to quit", 50, _screenHeight - 50, 20, Color.YELLOW);
    }

    private void DrawMineralCatalog()
    {
        int mainW = MainViewWidth;
        Raylib.DrawRectangle(0, 0, mainW, _screenHeight, new Color(10, 12, 22, 255));

        const int titleSize = 30;
        const int rowSize = 18;
        const int headerSize = 18;
        int titleW = UiText.MeasureText("MINERAL VALUE CHART", titleSize);
        UiText.DrawText("MINERAL VALUE CHART", (mainW - titleW) / 2, 36, titleSize, Color.SKYBLUE);

        const string subtitle = "Value in MU per unit of volume (manual appendix 7.1.1).";
        int subW = UiText.MeasureText(subtitle, headerSize);
        UiText.DrawText(
            subtitle,
            Math.Max(24, (mainW - subW) / 2),
            72,
            headerSize,
            new Color(160, 170, 195, 255));

        int y = 110;
        UiText.DrawText("Mineral", 32, y, headerSize, Color.GRAY);
        int priceHeaderX = mainW - UiText.MeasureText("MU", headerSize) - 32;
        UiText.DrawText("MU", priceHeaderX, y, headerSize, Color.GRAY);
        y += headerSize + 8;

        Raylib.DrawLine(24, y, mainW - 24, y, new Color(55, 65, 95, 255));
        y += 14;

        foreach (MineralTradeEntry m in _mineralTradeList)
        {
            UiText.DrawText(m.Name, 32, y, rowSize, Color.LIGHTGRAY);
            string priceText = m.Price.ToString("N0");
            int px = mainW - UiText.MeasureText(priceText, rowSize) - 32;
            UiText.DrawText(priceText, px, y, rowSize, Color.GOLD);
            y += rowSize + 4;
        }

        UiText.DrawText("Press ESC to return | X to quit", 24, _screenHeight - 36, 18, Color.YELLOW);
    }

    private void DrawShipManifest()
    {
        int mainW = MainViewWidth;
        Raylib.DrawRectangle(0, 0, mainW, _screenHeight, new Color(10, 12, 22, 255));

        const int titleSize = 30;
        const int rowSize = 18;
        const int headerSize = 18;
        int titleW = UiText.MeasureText("SHIP MANIFEST", titleSize);
        UiText.DrawText("SHIP MANIFEST", (mainW - titleW) / 2, 36, titleSize, Color.SKYBLUE);

        float totalUnits = 0f;
        for (int i = 0; i < _ship.Cargo.Count; i++)
        {
            CargoHoldEntry entry = _ship.Cargo[i];
            if (string.Equals(entry.Name, "Fuel", StringComparison.OrdinalIgnoreCase))
            {
                totalUnits += _ship.FuelQuantity;
            }
            else
            {
                totalUnits += entry.Quantity;
            }
        }

        string holdSummary = $"Hold: {totalUnits:F1} / {_ship.CargoCapacity} units ({_ship.GetCargoFillPercent()}% full)";
        int holdW = UiText.MeasureText(holdSummary, headerSize);
        UiText.DrawText(
            holdSummary,
            Math.Max(24, (mainW - holdW) / 2),
            72,
            headerSize,
            new Color(160, 170, 195, 255));

        int y = 110;
        int itemColX = 32;
        int qtyColX = mainW / 2 - 20;
        int categoryColX = mainW - 180;
        UiText.DrawText("Item", itemColX, y, headerSize, Color.GRAY);
        UiText.DrawText("Qty", qtyColX, y, headerSize, Color.GRAY);
        UiText.DrawText("Category", categoryColX, y, headerSize, Color.GRAY);
        y += headerSize + 8;

        Raylib.DrawLine(24, y, mainW - 24, y, new Color(55, 65, 95, 255));
        y += 14;

        for (int i = 0; i < _ship.Cargo.Count; i++)
        {
            CargoHoldEntry entry = _ship.Cargo[i];
            UiText.DrawText(entry.Name, itemColX, y, rowSize, Color.LIGHTGRAY);
            string qtyText = string.Equals(entry.Name, "Fuel", StringComparison.OrdinalIgnoreCase)
                ? _ship.FuelQuantity.ToString("F1")
                : entry.Quantity.ToString();
            UiText.DrawText(qtyText, qtyColX, y, rowSize, Color.WHITE);
            UiText.DrawText(entry.Category, categoryColX, y, rowSize, new Color(140, 180, 220, 255));
            y += rowSize + 4;
        }

        UiText.DrawText("Press ESC to return | M: Manifest | X to quit", 24, _screenHeight - 36, 18, Color.YELLOW);
    }

    private void DrawCanopyView()
    {
        int viewWidth = MainViewWidth;

        _parallax.Draw(_screenWidth, _screenHeight);
        _canopySystems.Draw(_ship, _starMap, viewWidth, _screenHeight, _currentState, _maneuverParallaxBoost, CanopyStarEnterRadiusPixels);

        const int frameThickness = 20;
        Color frameColor = new Color(40, 40, 45, 255);

        Raylib.DrawRectangle(0, 0, viewWidth, frameThickness, frameColor);
        Raylib.DrawRectangle(0, _screenHeight - frameThickness, viewWidth, frameThickness, frameColor);
        Raylib.DrawRectangle(0, 0, frameThickness, _screenHeight, frameColor);
        Raylib.DrawRectangle(viewWidth - frameThickness, 0, frameThickness, _screenHeight, frameColor);

        UiText.DrawText("CANOPY VIEW", 30, 30, 24, Color.WHITE);

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

        string coordText = $"Coordinates: ({_displayedCoordinates.X:F0}, {_displayedCoordinates.Y:F0})";
        int coordTextWidth = UiText.MeasureText(coordText, 18);
        int coordX = (viewWidth - coordTextWidth) / 2;
        UiText.DrawText(coordText, coordX, 60, 18, Color.SKYBLUE);

        bool thrusting = _ship.ManeuverThrustForward || _ship.ManeuverThrustReverse;
        bool coasting = !thrusting && _ship.Velocity.LengthSquared() > ManeuverVelocityStopEpsilonSq;

        if (thrusting)
        {
            UiText.DrawText("Engines: thrust", 30, 90, 18, Color.GREEN);
        }
        else if (coasting)
        {
            UiText.DrawText("Engines: coast", 30, 90, 18, new Color(230, 180, 80, 255));
        }
        else
        {
            UiText.DrawText("Engines: idle", 30, 90, 18, new Color(120, 120, 130, 255));
        }

        UiText.DrawText(
            "A/D: turn | W/S: thrust / reverse | C: combat | M: manifest | R: refuel | H: restore (debug)",
            30,
            _screenHeight - 50,
            16,
            Color.YELLOW);

        if (_rightPanel.MenuLevel == 0)
        {
            StarSystem? nearbyForHint = GetSystemNearCanopyCrosshair();
            if (nearbyForHint != null)
            {
                const int hintFontSize = 18;
                const int canopyStarRadius = 20;
                _canopySystems.GetStarScreenPosition(
                    _ship,
                    nearbyForHint,
                    viewWidth,
                    _screenHeight,
                    _maneuverParallaxBoost,
                    out int starScreenX,
                    out int starScreenY);

                string enterHint = $"Press SPACE / A to enter {nearbyForHint.Name}";
                int belowY = starScreenY + canopyStarRadius + 8;
                const int bottomFrameReserve = 78;
                float enterHintY;
                if (belowY + hintFontSize <= _screenHeight - bottomFrameReserve)
                {
                    enterHintY = belowY;
                }
                else
                {
                    enterHintY = starScreenY - canopyStarRadius - hintFontSize - 8;
                    enterHintY = Math.Max(8f, enterHintY);
                }

                UiText.DrawTextCenteredAtXClamped(
                    enterHint,
                    starScreenX,
                    enterHintY,
                    hintFontSize,
                    new Color(255, 230, 120, 255),
                    8f,
                    viewWidth - 8f);
            }
        }
    }

    private static bool WindowShouldClose()
    {
        bool closeRequested = Raylib.WindowShouldClose();

        if (closeRequested && InputManager.IsBackPressed())
        {
            return false;
        }

        return closeRequested;
    }
}
