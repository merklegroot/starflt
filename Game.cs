using Raylib_cs;
using System.Numerics;

namespace StarflightGame;

public enum GameState
{
    CanopyView,
    Maneuver,
    StarMap,
    PlanetaryExploration,
    ShipStatus
}

public class Game
{
    private readonly int screenWidth;
    private readonly int screenHeight;
    private GameState currentState = GameState.CanopyView;
    
    private StarSystem? currentSystem;
    private Planet? currentPlanet;
    private Ship ship;
    private StarMap starMap;
    public bool ShouldExit { get; private set; } = false;
    
    // Menu state
    private int selectedMenuIndex = 0;
    private int menuLevel = 0; // 0 = top level, 1 = submenu
    private readonly string[] topMenuItems = { "Captain", "Navigator" };
    private readonly string[] navigatorSubMenuItems = { "Manuever", "Starmap" };
    private bool justSwitchedState = false; // Flag to prevent key press propagation
    
    // Canopy view - starfield with parallax layers
    private class StarLayer
    {
        public List<Vector2> Stars { get; set; } = new List<Vector2>();
        public float SpeedMultiplier { get; set; } = 1.0f;
        public Color StarColor { get; set; } = Color.WHITE;
        public int StarSize { get; set; } = 1; // 0 = pixel, 1 = small circle, 2 = larger circle
    }
    
    private List<StarLayer> starfieldLayers = new List<StarLayer>();
    private Random starfieldRandom = new Random();
    private Vector2 previousShipPosition = Vector2.Zero;
    private Vector2 displayedCoordinates = Vector2.Zero;
    private int coordinateUpdateCounter = 0;

    public Game(int width, int height)
    {
        screenWidth = width;
        screenHeight = height;
        
        ship = new Ship();
        starMap = new StarMap();
        currentSystem = starMap.GetSystem(0);
        // Initialize ship at first system
        if (currentSystem != null)
        {
            ship.Position = currentSystem.Position;
        }
        
        // Initialize parallax starfield for canopy view
        GenerateParallaxStarfield();
    }

    public void Update()
    {
        // Check for quit key (X) in any state
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_X))
        {
            ShouldExit = true;
            return;
        }

        UpdateMenuNavigation();
        
        switch (currentState)
        {
            case GameState.CanopyView:
                UpdateCanopyView();
                break;
            case GameState.Maneuver:
                UpdateManeuver();
                break;
            case GameState.StarMap:
                UpdateStarMap();
                break;
            case GameState.PlanetaryExploration:
                UpdatePlanetaryExploration();
                break;
            case GameState.ShipStatus:
                UpdateShipStatus();
                break;
        }
    }

    private void UpdateCanopyView()
    {
        // Engines are not engaged - no movement
        // Can navigate to other views through menu
        // Navigator -> Starmap should switch to StarMap view
    }

    private void UpdateManeuver()
    {
        // Reset flag after first update
        if (justSwitchedState)
        {
            justSwitchedState = false;
            previousShipPosition = ship.Position;
            return; // Skip processing keys on the frame we switch states
        }
        
        // Handle ESC to return to CanopyView
        // Keep menu state (menuLevel and selectedMenuIndex) unchanged
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE))
        {
            currentState = GameState.CanopyView;
            // Keep menuLevel and selectedMenuIndex unchanged to preserve selection
            return;
        }
        
        // Ship movement in maneuver mode
        if (!ship.CanMove()) return;
        
        Vector2 movement = Vector2.Zero;
        float speed = ship.Speed;
        
        if (Raylib.IsKeyDown(KeyboardKey.KEY_W) || Raylib.IsKeyDown(KeyboardKey.KEY_UP))
            movement.Y -= speed;
        if (Raylib.IsKeyDown(KeyboardKey.KEY_S) || Raylib.IsKeyDown(KeyboardKey.KEY_DOWN))
            movement.Y += speed;
        if (Raylib.IsKeyDown(KeyboardKey.KEY_A) || Raylib.IsKeyDown(KeyboardKey.KEY_LEFT))
            movement.X -= speed;
        if (Raylib.IsKeyDown(KeyboardKey.KEY_D) || Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT))
            movement.X += speed;
        
        if (movement != Vector2.Zero)
        {
            // Update ship rotation to face movement direction
            // Atan2 gives angle where 0 = right, PI/2 = down, -PI/2 = up
            // Ship base points up (-PI/2), so add PI/2 to align properly
            ship.Rotation = MathF.Atan2(movement.Y, movement.X) + MathF.PI / 2.0f;
            
            ship.Position += movement;
            ship.ConsumeFuelForMovement();
            
            // Move starfield in opposite direction to create parallax effect
            // Each layer moves at its own speed multiplier for depth
            UpdateStarfieldMovement(-movement);
            
            // Update current system based on ship position
            currentSystem = starMap.GetSystemAtPosition(ship.Position);
        }
        
        previousShipPosition = ship.Position;
    }

    private void UpdateStarMap()
    {
        // Reset flag after first update
        if (justSwitchedState)
        {
            justSwitchedState = false;
            starMap.Update(ship);
            currentSystem = starMap.GetSystemAtPosition(ship.Position);
            return; // Skip processing keys on the frame we switch states
        }
        
        // Handle ESC to return to CanopyView
        // If in a menu submenu, keep menu state (menuLevel and selectedMenuIndex) unchanged
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE))
        {
            currentState = GameState.CanopyView;
            // Keep menuLevel and selectedMenuIndex unchanged to preserve selection
        }
        
        // Don't process ENTER if we're in a menu (menu handles it)
        if (menuLevel == 0 && Raylib.IsKeyPressed(KeyboardKey.KEY_ENTER) && currentSystem != null && currentSystem.Planets.Count > 0)
        {
            // Enter planetary exploration - select first planet
            currentPlanet = currentSystem.Planets[0];
            // Initialize ship position on planet surface
            ship.Position = currentPlanet.Position;
            currentState = GameState.PlanetaryExploration;
        }
        
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_I))
        {
            currentState = GameState.ShipStatus;
        }

        starMap.Update(ship);
        
        // Update current system based on ship position
        currentSystem = starMap.GetSystemAtPosition(ship.Position);
    }

    private void UpdatePlanetaryExploration()
    {
        // Only handle ESC for game state if not in a menu submenu
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE) && menuLevel == 0)
        {
            currentState = GameState.StarMap;
            currentPlanet = null;
        }

        if (currentPlanet != null)
        {
            currentPlanet.Update(ship);
        }
    }

    private void UpdateShipStatus()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE))
        {
            currentState = GameState.StarMap;
        }
    }

    private void UpdateMenuNavigation()
    {
        // Only allow menu navigation when not in ShipStatus screen
        if (currentState == GameState.ShipStatus)
            return;
        
        // Handle ESC to go back to previous menu level
        // But don't handle ESC in StarMap or Maneuver mode - let those states handle it
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE) && currentState != GameState.StarMap && currentState != GameState.Maneuver)
        {
            if (menuLevel > 0)
            {
                menuLevel = 0;
                selectedMenuIndex = 0;
            }
            // If at top level, ESC is handled by game state (don't interfere)
            return;
        }
        
        // Get current menu items based on level
        string[] currentMenuItems = menuLevel == 0 ? topMenuItems : navigatorSubMenuItems;
        
        // Navigate menu with arrow keys (but not in StarMap or Maneuver mode where arrows move ship/camera)
        if (currentState != GameState.StarMap && currentState != GameState.Maneuver)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_UP))
            {
                selectedMenuIndex = Math.Max(0, selectedMenuIndex - 1);
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.KEY_DOWN))
            {
                selectedMenuIndex = Math.Min(currentMenuItems.Length - 1, selectedMenuIndex + 1);
            }
        }
        
        // Number keys for direct selection (only at top level)
        if (menuLevel == 0)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_ONE))
            {
                selectedMenuIndex = 0;
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.KEY_TWO))
            {
                selectedMenuIndex = 1;
            }
        }
        
        // Handle selection with SPACE or ENTER
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_SPACE) || Raylib.IsKeyPressed(KeyboardKey.KEY_ENTER))
        {
            if (menuLevel == 0)
            {
                // Top level selection
                if (selectedMenuIndex == 0) // Captain
                {
                    // TODO: Handle Captain menu
                }
                else if (selectedMenuIndex == 1) // Navigator
                {
                    // Enter Navigator submenu
                    menuLevel = 1;
                    selectedMenuIndex = 0;
                }
            }
            else if (menuLevel == 1)
            {
                // Navigator submenu selection
                if (selectedMenuIndex == 0) // Manuever
                {
                    // Switch to Maneuver mode
                    currentState = GameState.Maneuver;
                    // Keep menu state: stay in Navigator submenu with Maneuver selected
                    // menuLevel stays at 1, selectedMenuIndex stays at 0
                    justSwitchedState = true; // Set flag to prevent key propagation
                }
                else if (selectedMenuIndex == 1) // Starmap
                {
                    // Switch to StarMap view
                    currentState = GameState.StarMap;
                    // Keep menu state: stay in Navigator submenu with Starmap selected
                    // menuLevel stays at 1, selectedMenuIndex stays at 1
                    justSwitchedState = true; // Set flag to prevent key propagation
                }
            }
        }
        
        // Clamp menu index
        selectedMenuIndex = Math.Clamp(selectedMenuIndex, 0, currentMenuItems.Length - 1);
    }

    public void Draw()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.BLACK);

        switch (currentState)
        {
            case GameState.CanopyView:
                DrawCanopyView();
                DrawRightPanel();
                break;
            case GameState.Maneuver:
                DrawCanopyView();
                DrawRightPanel();
                break;
            case GameState.StarMap:
                starMap.Draw(screenWidth - 250, screenHeight, ship);
                DrawUI();
                DrawRightPanel();
                break;
            case GameState.PlanetaryExploration:
                if (currentPlanet != null)
                {
                    currentPlanet.Draw(screenWidth - 250, screenHeight, ship);
                }
                DrawPlanetaryUI();
                DrawRightPanel();
                break;
            case GameState.ShipStatus:
                DrawShipStatus();
                break;
        }

        Raylib.EndDrawing();
    }

    private void DrawUI()
    {
        // Fuel and resources display
        Raylib.DrawText($"Fuel: {ship.Fuel:F1}%", 10, 10, 20, Color.WHITE);
        Raylib.DrawText($"Credits: {ship.Credits}", 10, 35, 20, Color.WHITE);
        if (currentSystem != null && currentSystem.Planets.Count > 0)
        {
            Raylib.DrawText($"Press ENTER to explore {currentSystem.Planets[0].Name} | I for ship status | X to quit", 10, screenHeight - 30, 16, Color.YELLOW);
        }
        else
        {
            Raylib.DrawText($"Press I for ship status | TAB to warp to nearest system | X to quit", 10, screenHeight - 30, 16, Color.YELLOW);
        }
    }

    private void DrawPlanetaryUI()
    {
        Raylib.DrawText($"Fuel: {ship.Fuel:F1}%", 10, 10, 20, Color.WHITE);
        Raylib.DrawText($"Minerals: {ship.Minerals}", 10, 35, 20, Color.WHITE);
        Raylib.DrawText($"Press ESC to return to star map | X to quit", 10, screenHeight - 30, 16, Color.YELLOW);
        
        if (currentPlanet != null)
        {
            var mineral = currentPlanet.GetMineralAt(ship.Position);
            if (mineral != null)
            {
                Raylib.DrawText($"Mineral detected: {mineral.Name} ({mineral.Value} credits)", 10, 60, 18, Color.GREEN);
                Raylib.DrawText("Press SPACE to mine", 10, 85, 18, Color.YELLOW);
            }
        }
    }

    private void DrawShipStatus()
    {
        int startY = 50;
        int lineHeight = 30;
        
        Raylib.DrawText("SHIP STATUS", screenWidth / 2 - 100, startY, 32, Color.WHITE);
        
        startY += 60;
        Raylib.DrawText($"Fuel: {ship.Fuel:F1}%", 50, startY, 24, Color.WHITE);
        startY += lineHeight;
        Raylib.DrawText($"Credits: {ship.Credits}", 50, startY, 24, Color.WHITE);
        startY += lineHeight;
        Raylib.DrawText($"Minerals: {ship.Minerals}", 50, startY, 24, Color.WHITE);
        startY += lineHeight;
        Raylib.DrawText($"Position: ({ship.Position.X:F1}, {ship.Position.Y:F1})", 50, startY, 24, Color.WHITE);
        
        Raylib.DrawText("Press ESC to return | X to quit", 50, screenHeight - 50, 20, Color.YELLOW);
    }

    private void DrawRightPanel()
    {
        const int panelWidth = 250;
        int panelX = screenWidth - panelWidth;
        const int panelPadding = 15;
        const int textFontSize = 18;
        const int menuFontSize = 20;
        const int lineSpacing = 25;

        // Draw panel background
        Raylib.DrawRectangle(panelX, 0, panelWidth, screenHeight, new Color(30, 30, 35, 255));
        
        // Draw panel border
        Raylib.DrawLine(panelX, 0, panelX, screenHeight, Color.DARKGRAY);
        
        int yPos = panelPadding;
        
        // Draw menu
        DrawMenu(panelX, ref yPos, panelWidth, panelPadding, menuFontSize, lineSpacing);
        
        // Draw separator line
        yPos += 10;
        Raylib.DrawLine(panelX + panelPadding, yPos, panelX + panelWidth - panelPadding, yPos, Color.DARKGRAY);
        
        // Draw ship status information
        yPos += 20;
        
        // Fuel with color coding
        Raylib.DrawText("Fuel:", panelX + panelPadding, yPos, textFontSize, Color.WHITE);
        Color fuelColor = ship.Fuel > 50 ? Color.GREEN : ship.Fuel > 25 ? Color.YELLOW : Color.RED;
        Raylib.DrawText($"{ship.Fuel:F1}%", panelX + panelPadding + 70, yPos, textFontSize, fuelColor);
        yPos += lineSpacing;
        
        // Credits
        Raylib.DrawText("Credits:", panelX + panelPadding, yPos, textFontSize, Color.WHITE);
        Raylib.DrawText($"{ship.Credits:N0}", panelX + panelPadding + 80, yPos, textFontSize, Color.GOLD);
        yPos += lineSpacing;
        
        // Minerals
        Raylib.DrawText("Minerals:", panelX + panelPadding, yPos, textFontSize, Color.WHITE);
        Raylib.DrawText($"{ship.Minerals}", panelX + panelPadding + 90, yPos, textFontSize, Color.LIGHTGRAY);
        yPos += lineSpacing;
        
        // Speed
        Raylib.DrawText("Speed:", panelX + panelPadding, yPos, textFontSize, Color.WHITE);
        Raylib.DrawText($"{ship.Speed:F1}", panelX + panelPadding + 70, yPos, textFontSize, Color.SKYBLUE);
        yPos += lineSpacing;
        
        // Position
        yPos += 10;
        Raylib.DrawText("Position:", panelX + panelPadding, yPos, textFontSize, Color.WHITE);
        yPos += lineSpacing;
        Raylib.DrawText($"X: {ship.Position.X:F1}", panelX + panelPadding + 10, yPos, textFontSize - 2, Color.LIGHTGRAY);
        yPos += lineSpacing - 5;
            Raylib.DrawText($"Y: {ship.Position.Y:F1}", panelX + panelPadding + 10, yPos, textFontSize - 2, Color.LIGHTGRAY);
    }

    private void GenerateParallaxStarfield()
    {
        starfieldLayers.Clear();
        const int panelWidth = 250; // Match the panel width
        int viewWidth = screenWidth - panelWidth;
        
        // Layer 1: Far stars (slowest, dimmest, smallest)
        var farLayer = new StarLayer
        {
            SpeedMultiplier = 8.0f, // 2.0f * 4
            StarColor = new Color(150, 150, 150, 255), // Dim gray
            StarSize = 0 // Pixels
        };
        for (int i = 0; i < 150; i++)
        {
            farLayer.Stars.Add(new Vector2(
                starfieldRandom.Next(0, viewWidth),
                starfieldRandom.Next(0, screenHeight)
            ));
        }
        starfieldLayers.Add(farLayer);
        
        // Layer 2: Mid stars (medium speed, medium brightness)
        var midLayer = new StarLayer
        {
            SpeedMultiplier = 20.0f, // 5.0f * 4
            StarColor = Color.WHITE,
            StarSize = 0 // Pixels
        };
        for (int i = 0; i < 100; i++)
        {
            midLayer.Stars.Add(new Vector2(
                starfieldRandom.Next(0, viewWidth),
                starfieldRandom.Next(0, screenHeight)
            ));
        }
        starfieldLayers.Add(midLayer);
        
        // Layer 3: Close stars (fastest, brightest, larger)
        var closeLayer = new StarLayer
        {
            SpeedMultiplier = 48.0f, // 12.0f * 4
            StarColor = Color.WHITE,
            StarSize = 1 // Small circles
        };
        for (int i = 0; i < 50; i++)
        {
            closeLayer.Stars.Add(new Vector2(
                starfieldRandom.Next(0, viewWidth),
                starfieldRandom.Next(0, screenHeight)
            ));
        }
        starfieldLayers.Add(closeLayer);
    }

    private void UpdateStarfieldMovement(Vector2 movement)
    {
        const int panelWidth = 250;
        int viewWidth = screenWidth - panelWidth;
        
        // Update each parallax layer at different speeds
        foreach (var layer in starfieldLayers)
        {
            Vector2 layerMovement = movement * layer.SpeedMultiplier;
            
            for (int i = 0; i < layer.Stars.Count; i++)
            {
                Vector2 star = layer.Stars[i];
                star += layerMovement;
                
                // Wrap stars around when they go off screen
                if (star.X < 0)
                    star.X = viewWidth;
                else if (star.X > viewWidth)
                    star.X = 0;
                
                if (star.Y < 0)
                    star.Y = screenHeight;
                else if (star.Y > screenHeight)
                    star.Y = 0;
                
                layer.Stars[i] = star;
            }
        }
    }

    private void DrawCanopyView()
    {
        const int panelWidth = 250;
        int viewWidth = screenWidth - panelWidth;
        
        // Draw parallax starfield layers (far to near for proper depth)
        foreach (var layer in starfieldLayers)
        {
            foreach (var star in layer.Stars)
            {
                if (layer.StarSize == 0)
                {
                    // Draw as pixel
                    Raylib.DrawPixel((int)star.X, (int)star.Y, layer.StarColor);
                }
                else if (layer.StarSize == 1)
                {
                    // Draw as small circle
                    Raylib.DrawCircle((int)star.X, (int)star.Y, 1, layer.StarColor);
                }
                else
                {
                    // Draw as larger circle
                    Raylib.DrawCircle((int)star.X, (int)star.Y, 2, layer.StarColor);
                }
            }
        }
        
        // Draw ship in the center
        int shipCenterX = viewWidth / 2;
        int shipCenterY = screenHeight / 2;
        // In Maneuver mode, use ship's rotation; otherwise point up
        float rotation = currentState == GameState.Maneuver ? ship.Rotation : -MathF.PI / 2.0f;
        DrawShip(shipCenterX, shipCenterY, rotation);
        
        // Draw canopy frame/border to simulate ship window
        int frameThickness = 20;
        Color frameColor = new Color(40, 40, 45, 255);
        
        // Top frame
        Raylib.DrawRectangle(0, 0, viewWidth, frameThickness, frameColor);
        // Bottom frame
        Raylib.DrawRectangle(0, screenHeight - frameThickness, viewWidth, frameThickness, frameColor);
        // Left frame
        Raylib.DrawRectangle(0, 0, frameThickness, screenHeight, frameColor);
        // Right frame (before panel)
        Raylib.DrawRectangle(viewWidth - frameThickness, 0, frameThickness, screenHeight, frameColor);
        
        // Draw status text
        Raylib.DrawText("CANOPY VIEW", 30, 30, 24, Color.WHITE);
        
        // Draw coordinates at the top center (update slowly, rounded to integers)
        if (currentState == GameState.Maneuver)
        {
            // Gradually move displayed coordinates towards actual position, rounded to nearest integer
            Vector2 targetPos = new Vector2(
                MathF.Round(ship.Position.X),
                MathF.Round(ship.Position.Y)
            );
            
            // Move displayed coordinates by 1 unit at a time towards target
            if (MathF.Abs(displayedCoordinates.X - targetPos.X) >= 1.0f)
            {
                displayedCoordinates.X += MathF.Sign(targetPos.X - displayedCoordinates.X);
            }
            if (MathF.Abs(displayedCoordinates.Y - targetPos.Y) >= 1.0f)
            {
                displayedCoordinates.Y += MathF.Sign(targetPos.Y - displayedCoordinates.Y);
            }
        }
        else
        {
            displayedCoordinates = new Vector2(
                MathF.Round(ship.Position.X),
                MathF.Round(ship.Position.Y)
            );
        }
        
        string coordText = $"Coordinates: ({displayedCoordinates.X:F0}, {displayedCoordinates.Y:F0})";
        int coordTextWidth = Raylib.MeasureText(coordText, 18);
        int coordX = (viewWidth - coordTextWidth) / 2;
        Raylib.DrawText(coordText, coordX, 60, 18, Color.SKYBLUE);
        
        // Show engine status based on current state
        if (currentState == GameState.Maneuver)
        {
            Raylib.DrawText("Engines: ON", 30, 90, 18, Color.GREEN);
            Raylib.DrawText("WASD/Arrow Keys: Move | ESC: Disengage", 30, screenHeight - 50, 16, Color.YELLOW);
        }
        else
        {
            Raylib.DrawText("Engines: OFF", 30, 90, 18, Color.RED);
            Raylib.DrawText("Use Navigator menu to access Starmap", 30, screenHeight - 50, 16, Color.YELLOW);
        }
    }

    private void DrawShip(int centerX, int centerY, float rotation)
    {
        // Rotate points around the center
        Vector2 RotatePoint(Vector2 point, Vector2 center, float angle)
        {
            float cos = MathF.Cos(angle);
            float sin = MathF.Sin(angle);
            float dx = point.X - center.X;
            float dy = point.Y - center.Y;
            return new Vector2(
                center.X + dx * cos - dy * sin,
                center.Y + dx * sin + dy * cos
            );
        }
        
        Vector2 center = new Vector2(centerX, centerY);
        
        // Base ship points (pointing up, -90 degrees)
        Vector2[] baseShipPoints = new Vector2[]
        {
            new Vector2(centerX, centerY - 30),      // Top point (nose)
            new Vector2(centerX - 25, centerY + 20), // Bottom left
            new Vector2(centerX + 25, centerY + 20)  // Bottom right
        };
        
        // Rotate ship points (rotation already includes the offset from base orientation)
        Vector2[] shipPoints = new Vector2[]
        {
            RotatePoint(baseShipPoints[0], center, rotation),
            RotatePoint(baseShipPoints[1], center, rotation),
            RotatePoint(baseShipPoints[2], center, rotation)
        };
        
        // Draw ship body
        Raylib.DrawTriangle(shipPoints[0], shipPoints[1], shipPoints[2], Color.GRAY);
        Raylib.DrawTriangleLines(shipPoints[0], shipPoints[1], shipPoints[2], Color.WHITE);
        
        // Draw cockpit window
        Vector2[] baseCockpitPoints = new Vector2[]
        {
            new Vector2(centerX, centerY - 15),
            new Vector2(centerX - 8, centerY - 5),
            new Vector2(centerX + 8, centerY - 5)
        };
        
        Vector2[] cockpitPoints = new Vector2[]
        {
            RotatePoint(baseCockpitPoints[0], center, rotation),
            RotatePoint(baseCockpitPoints[1], center, rotation),
            RotatePoint(baseCockpitPoints[2], center, rotation)
        };
        Raylib.DrawTriangle(cockpitPoints[0], cockpitPoints[1], cockpitPoints[2], new Color(50, 100, 150, 200));
        
        // Draw engine nozzles at the back (rotated)
        Vector2[] enginePositions = new Vector2[]
        {
            new Vector2(centerX - 20, centerY + 20),
            new Vector2(centerX + 12, centerY + 20)
        };
        
        foreach (var enginePos in enginePositions)
        {
            Vector2 rotatedPos = RotatePoint(enginePos, center, rotation);
            // Draw rotated rectangle (simplified as a small rectangle)
            // For simplicity, we'll draw a small square at the rotated position
            Raylib.DrawRectangle((int)rotatedPos.X - 4, (int)rotatedPos.Y - 6, 8, 12, Color.DARKGRAY);
        }
        
        // Draw some detail lines (rotated)
        Vector2 lineStart = RotatePoint(new Vector2(centerX - 15, centerY + 5), center, rotation);
        Vector2 lineEnd = RotatePoint(new Vector2(centerX + 15, centerY + 5), center, rotation);
        Raylib.DrawLine((int)lineStart.X, (int)lineStart.Y, (int)lineEnd.X, (int)lineEnd.Y, Color.DARKGRAY);
    }

    private void DrawMenu(int panelX, ref int yPos, int panelWidth, int panelPadding, int menuFontSize, int lineSpacing)
    {
        // Draw menu title
        string menuTitle = menuLevel == 0 ? "MENU" : "NAVIGATOR";
        Raylib.DrawText(menuTitle, panelX + panelPadding, yPos, menuFontSize, Color.WHITE);
        yPos += menuFontSize + 15;
        
        // Get current menu items based on level
        string[] currentMenuItems = menuLevel == 0 ? topMenuItems : navigatorSubMenuItems;
        
        // Draw menu items
        for (int i = 0; i < currentMenuItems.Length; i++)
        {
            Color itemColor = i == selectedMenuIndex ? Color.YELLOW : Color.LIGHTGRAY;
            Color bgColor = i == selectedMenuIndex ? new Color(60, 60, 70, 255) : Color.BLANK;
            
            // Draw selection background
            if (i == selectedMenuIndex)
            {
                Raylib.DrawRectangle(panelX + panelPadding - 5, yPos - 2, panelWidth - panelPadding * 2 + 10, menuFontSize + 4, bgColor);
            }
            
            // Check if this menu item is active
            bool isActive = IsMenuItemActive(menuLevel, i);
            string activeIndicator = isActive ? "● " : "";
            
            // Draw menu item
            string prefix = menuLevel == 0 ? $"{i + 1}. " : "  ";
            Raylib.DrawText($"{prefix}{activeIndicator}{currentMenuItems[i]}", panelX + panelPadding, yPos, menuFontSize, itemColor);
            yPos += lineSpacing;
        }
        
        // Draw navigation hints
        yPos += 10;
        if (menuLevel > 0)
        {
            Raylib.DrawText("ESC: Back", panelX + panelPadding, yPos, menuFontSize - 4, Color.DARKGRAY);
        }
        else
        {
            Raylib.DrawText("SPACE/ENTER: Select", panelX + panelPadding, yPos, menuFontSize - 4, Color.DARKGRAY);
        }
    }

    private bool IsMenuItemActive(int level, int index)
    {
        if (level == 1) // Navigator submenu
        {
            if (index == 0) // Maneuver
            {
                return currentState == GameState.Maneuver;
            }
            if (index == 1) // Starmap
            {
                return currentState == GameState.StarMap;
            }
        }
        // Add checks for other menu levels as needed
        return false;
    }
}
