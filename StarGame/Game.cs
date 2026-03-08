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
    private StarMapView starMap;
    public bool ShouldExit { get; private set; } = false;
    
    // Menu state
    private int selectedMenuIndex = 0;
    private int menuLevel = 0; // 0 = top level, 1 = submenu
    private readonly string[] topMenuItems = { "Captain", "Navigator" };
    private readonly string[] navigatorSubMenuItems = { "Manuever", "Starmap" };
    private bool justSwitchedState = false; // Flag to prevent key press propagation
    
    // Canopy view - starfield with parallax layers
    private class Star
    {
        public Vector2 Position { get; set; }
        public float Brightness { get; set; } = 1.0f;
        public float TwinklePhase { get; set; } = 0.0f;
        public float TwinkleSpeed { get; set; } = 0.0f;
        public Color BaseColor { get; set; } = Color.WHITE;
        public float Size { get; set; } = 1.0f;
    }
    
    private class StarLayer
    {
        public List<Star> Stars { get; set; } = new List<Star>();
        public float SpeedMultiplier { get; set; } = 1.0f;
        public Color BaseColor { get; set; } = Color.WHITE;
        public float MinBrightness { get; set; } = 0.3f;
        public float MaxBrightness { get; set; } = 1.0f;
        public bool EnableTwinkle { get; set; } = false;
    }
    
    private List<StarLayer> starfieldLayers = new List<StarLayer>();
    private Random starfieldRandom = new Random();
    private Vector2 previousShipPosition = Vector2.Zero;
    private Vector2 displayedCoordinates = Vector2.Zero;
    private int coordinateUpdateCounter = 0;
    private float twinkleTime = 0.0f;

    public Game(int width, int height)
    {
        screenWidth = width;
        screenHeight = height;
        
        ship = new Ship();
        starMap = new StarMapView();
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
        
        // Update starfield twinkling even when stationary
        UpdateStarfieldTwinkling();
    }
    
    private void UpdateStarfieldTwinkling()
    {
        // Update twinkle time
        twinkleTime += 0.016f; // Approximate frame time
        
        // Update twinkling for all layers
        foreach (var layer in starfieldLayers)
        {
            if (!layer.EnableTwinkle) continue;
            
            for (int i = 0; i < layer.Stars.Count; i++)
            {
                Star star = layer.Stars[i];
                if (star.TwinkleSpeed > 0)
                {
                    star.TwinklePhase += star.TwinkleSpeed * 0.016f;
                    float twinkle = (MathF.Sin(star.TwinklePhase) + 1.0f) * 0.5f; // 0 to 1
                    star.Brightness = layer.MinBrightness + (layer.MaxBrightness - layer.MinBrightness) * twinkle;
                    layer.Stars[i] = star;
                }
            }
        }
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
        
        // Update starfield twinkling
        UpdateStarfieldTwinkling();
        
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
            // Check if the currently selected item is active - if so, exit it
            bool isCurrentlyActive = IsMenuItemActive(menuLevel, selectedMenuIndex);
            if (isCurrentlyActive)
            {
                // Exit the active item by returning to CanopyView
                currentState = GameState.CanopyView;
                justSwitchedState = true;
                return;
            }
            
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
        
        // Layer 1: Very far stars (slowest, dimmest, smallest, many)
        var veryFarLayer = new StarLayer
        {
            SpeedMultiplier = 4.0f,
            BaseColor = new Color(100, 100, 120, 255),
            MinBrightness = 0.2f,
            MaxBrightness = 0.5f,
            EnableTwinkle = true
        };
        for (int i = 0; i < 300; i++)
        {
            veryFarLayer.Stars.Add(new Star
            {
                Position = new Vector2(
                    starfieldRandom.Next(0, viewWidth),
                    starfieldRandom.Next(0, screenHeight)
                ),
                Brightness = (float)(starfieldRandom.NextDouble() * 0.3 + 0.2),
                TwinklePhase = (float)(starfieldRandom.NextDouble() * Math.PI * 2),
                TwinkleSpeed = (float)(starfieldRandom.NextDouble() * 0.5 + 0.3),
                BaseColor = new Color(100, 100, 120, 255),
                Size = 0.5f
            });
        }
        starfieldLayers.Add(veryFarLayer);
        
        // Layer 2: Far stars (slow, dim, small)
        var farLayer = new StarLayer
        {
            SpeedMultiplier = 8.0f,
            BaseColor = new Color(150, 150, 160, 255),
            MinBrightness = 0.4f,
            MaxBrightness = 0.7f,
            EnableTwinkle = true
        };
        for (int i = 0; i < 200; i++)
        {
            farLayer.Stars.Add(new Star
            {
                Position = new Vector2(
                    starfieldRandom.Next(0, viewWidth),
                    starfieldRandom.Next(0, screenHeight)
                ),
                Brightness = (float)(starfieldRandom.NextDouble() * 0.3 + 0.4),
                TwinklePhase = (float)(starfieldRandom.NextDouble() * Math.PI * 2),
                TwinkleSpeed = (float)(starfieldRandom.NextDouble() * 0.8 + 0.5),
                BaseColor = new Color(150, 150, 160, 255),
                Size = 0.8f
            });
        }
        starfieldLayers.Add(farLayer);
        
        // Layer 3: Mid stars (medium speed, medium brightness, varied colors)
        var midLayer = new StarLayer
        {
            SpeedMultiplier = 20.0f,
            BaseColor = Color.WHITE,
            MinBrightness = 0.6f,
            MaxBrightness = 1.0f,
            EnableTwinkle = true
        };
        for (int i = 0; i < 150; i++)
        {
            // Vary star colors (white, blue-white, yellow-white)
            Color starColor = Color.WHITE;
            float colorChoice = (float)starfieldRandom.NextDouble();
            if (colorChoice < 0.3f)
                starColor = new Color(200, 220, 255, 255); // Blue-white
            else if (colorChoice < 0.6f)
                starColor = new Color(255, 250, 200, 255); // Yellow-white
            else
                starColor = Color.WHITE;
            
            midLayer.Stars.Add(new Star
            {
                Position = new Vector2(
                    starfieldRandom.Next(0, viewWidth),
                    starfieldRandom.Next(0, screenHeight)
                ),
                Brightness = (float)(starfieldRandom.NextDouble() * 0.4 + 0.6),
                TwinklePhase = (float)(starfieldRandom.NextDouble() * Math.PI * 2),
                TwinkleSpeed = (float)(starfieldRandom.NextDouble() * 1.0 + 0.7),
                BaseColor = starColor,
                Size = 1.0f
            });
        }
        starfieldLayers.Add(midLayer);
        
        // Layer 4: Close stars (fast, bright, larger, colorful)
        var closeLayer = new StarLayer
        {
            SpeedMultiplier = 48.0f,
            BaseColor = Color.WHITE,
            MinBrightness = 0.8f,
            MaxBrightness = 1.0f,
            EnableTwinkle = false
        };
        for (int i = 0; i < 80; i++)
        {
            // More varied colors for close stars
            Color starColor = Color.WHITE;
            float colorChoice = (float)starfieldRandom.NextDouble();
            if (colorChoice < 0.25f)
                starColor = new Color(180, 200, 255, 255); // Blue
            else if (colorChoice < 0.5f)
                starColor = new Color(255, 240, 180, 255); // Yellow
            else if (colorChoice < 0.7f)
                starColor = new Color(255, 200, 200, 255); // Red-white
            else
                starColor = Color.WHITE;
            
            closeLayer.Stars.Add(new Star
            {
                Position = new Vector2(
                    starfieldRandom.Next(0, viewWidth),
                    starfieldRandom.Next(0, screenHeight)
                ),
                Brightness = (float)(starfieldRandom.NextDouble() * 0.2 + 0.8),
                TwinklePhase = 0.0f,
                TwinkleSpeed = 0.0f,
                BaseColor = starColor,
                Size = (float)(starfieldRandom.NextDouble() * 1.5 + 1.5) // 1.5 to 3.0
            });
        }
        starfieldLayers.Add(closeLayer);
        
        // Layer 5: Very close stars (fastest, brightest, largest, rare)
        var veryCloseLayer = new StarLayer
        {
            SpeedMultiplier = 80.0f,
            BaseColor = Color.WHITE,
            MinBrightness = 1.0f,
            MaxBrightness = 1.0f,
            EnableTwinkle = false
        };
        for (int i = 0; i < 20; i++)
        {
            // Bright, colorful close stars
            Color starColor = Color.WHITE;
            float colorChoice = (float)starfieldRandom.NextDouble();
            if (colorChoice < 0.3f)
                starColor = new Color(150, 180, 255, 255); // Bright blue
            else if (colorChoice < 0.6f)
                starColor = new Color(255, 220, 150, 255); // Bright yellow
            else
                starColor = Color.WHITE;
            
            veryCloseLayer.Stars.Add(new Star
            {
                Position = new Vector2(
                    starfieldRandom.Next(0, viewWidth),
                    starfieldRandom.Next(0, screenHeight)
                ),
                Brightness = 1.0f,
                TwinklePhase = 0.0f,
                TwinkleSpeed = 0.0f,
                BaseColor = starColor,
                Size = (float)(starfieldRandom.NextDouble() * 2.0 + 3.0) // 3.0 to 5.0
            });
        }
        starfieldLayers.Add(veryCloseLayer);
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
                Star star = layer.Stars[i];
                
                // Update position
                star.Position += layerMovement;
                
                // Update twinkling
                if (layer.EnableTwinkle && star.TwinkleSpeed > 0)
                {
                    star.TwinklePhase += star.TwinkleSpeed * 0.016f;
                    float twinkle = (MathF.Sin(star.TwinklePhase) + 1.0f) * 0.5f; // 0 to 1
                    star.Brightness = layer.MinBrightness + (layer.MaxBrightness - layer.MinBrightness) * twinkle;
                }
                
                // Wrap stars around when they go off screen
                Vector2 pos = star.Position;
                if (pos.X < 0)
                    pos.X = viewWidth;
                else if (pos.X > viewWidth)
                    pos.X = 0;
                
                if (pos.Y < 0)
                    pos.Y = screenHeight;
                else if (pos.Y > screenHeight)
                    pos.Y = 0;
                
                star.Position = pos;
                
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
                // Calculate final color with brightness
                Color finalColor = new Color(
                    (byte)(star.BaseColor.R * star.Brightness),
                    (byte)(star.BaseColor.G * star.Brightness),
                    (byte)(star.BaseColor.B * star.Brightness),
                    star.BaseColor.A
                );
                
                float size = star.Size;
                
                if (size < 1.0f)
                {
                    // Very small stars - draw as pixel
                    Raylib.DrawPixel((int)star.Position.X, (int)star.Position.Y, finalColor);
                }
                else if (size < 2.0f)
                {
                    // Small stars - draw as small circle
                    Raylib.DrawCircle((int)star.Position.X, (int)star.Position.Y, 1, finalColor);
                }
                else if (size < 3.5f)
                {
                    // Medium stars - draw with glow effect
                    // Outer glow (dimmer)
                    Color glowColor = new Color(finalColor.R, finalColor.G, finalColor.B, (byte)(finalColor.A * 0.3f));
                    Raylib.DrawCircle((int)star.Position.X, (int)star.Position.Y, (int)size + 1, glowColor);
                    // Core
                    Raylib.DrawCircle((int)star.Position.X, (int)star.Position.Y, (int)size, finalColor);
                }
                else
                {
                    // Large bright stars - draw with multiple glow layers
                    // Outer glow (very dim)
                    Color outerGlow = new Color(finalColor.R, finalColor.G, finalColor.B, (byte)(finalColor.A * 0.2f));
                    Raylib.DrawCircle((int)star.Position.X, (int)star.Position.Y, (int)size + 2, outerGlow);
                    // Mid glow
                    Color midGlow = new Color(finalColor.R, finalColor.G, finalColor.B, (byte)(finalColor.A * 0.5f));
                    Raylib.DrawCircle((int)star.Position.X, (int)star.Position.Y, (int)size + 1, midGlow);
                    // Core
                    Raylib.DrawCircle((int)star.Position.X, (int)star.Position.Y, (int)size, finalColor);
                    // Bright center
                    Color brightCenter = new Color((byte)255, (byte)255, (byte)255, (byte)(finalColor.A * 0.8f));
                    Raylib.DrawCircle((int)star.Position.X, (int)star.Position.Y, (int)(size * 0.5f), brightCenter);
                }
            }
        }
        
        // Draw star systems (much larger than starfield stars)
        int shipCenterX = viewWidth / 2;
        int shipCenterY = screenHeight / 2;
        var systems = starMap.GetAllSystems();
        
        foreach (var system in systems)
        {
            // Calculate screen position relative to ship (ship is centered)
            Vector2 relativePos = system.Position - ship.Position;
            int screenX = shipCenterX + (int)relativePos.X;
            int screenY = shipCenterY + (int)relativePos.Y;
            
            // Only draw if within reasonable bounds (with some margin for glow)
            if (screenX > -50 && screenX < viewWidth + 50 && screenY > -50 && screenY < screenHeight + 50)
            {
                const int starRadius = 20; // Much larger than starfield stars
                const int starburstSize = 35; // Size of the diamond starburst
                const float curvature = 2.5f; // Controls how much the edges curve inward (very extreme)
                
                // Draw diamond-shaped starburst with curved edges (edges curve toward center)
                Color fillColor = new Color((byte)255, (byte)255, (byte)255, (byte)60); // More transparent white
                Color starburstColor = fillColor; // Outline matches fill color
                
                // Diamond corners (rotated 45 degrees)
                Vector2 top = new Vector2(screenX, screenY - starburstSize);
                Vector2 right = new Vector2(screenX + starburstSize, screenY);
                Vector2 bottom = new Vector2(screenX, screenY + starburstSize);
                Vector2 left = new Vector2(screenX - starburstSize, screenY);
                
                // Build points for the curved diamond shape
                int segments = 40;
                List<Vector2> diamondPoints = new List<Vector2>();
                Vector2 center = new Vector2(screenX, screenY);
                
                // Top to right edge (curved inward)
                for (int i = 0; i <= segments; i++)
                {
                    float t = (float)i / segments;
                    Vector2 p = Vector2.Lerp(top, right, t);
                    float dist = Vector2.Distance(p, center);
                    float curve = curvature * (starburstSize - dist) / starburstSize;
                    Vector2 dir = Vector2.Normalize(center - p);
                    p += dir * curve * starburstSize;
                    diamondPoints.Add(p);
                }
                
                // Right to bottom edge
                for (int i = 1; i <= segments; i++)
                {
                    float t = (float)i / segments;
                    Vector2 p = Vector2.Lerp(right, bottom, t);
                    float dist = Vector2.Distance(p, center);
                    float curve = curvature * (starburstSize - dist) / starburstSize;
                    Vector2 dir = Vector2.Normalize(center - p);
                    p += dir * curve * starburstSize;
                    diamondPoints.Add(p);
                }
                
                // Bottom to left edge
                for (int i = 1; i <= segments; i++)
                {
                    float t = (float)i / segments;
                    Vector2 p = Vector2.Lerp(bottom, left, t);
                    float dist = Vector2.Distance(p, center);
                    float curve = curvature * (starburstSize - dist) / starburstSize;
                    Vector2 dir = Vector2.Normalize(center - p);
                    p += dir * curve * starburstSize;
                    diamondPoints.Add(p);
                }
                
                // Left to top edge
                for (int i = 1; i < segments; i++)
                {
                    float t = (float)i / segments;
                    Vector2 p = Vector2.Lerp(left, top, t);
                    float dist = Vector2.Distance(p, center);
                    float curve = curvature * (starburstSize - dist) / starburstSize;
                    Vector2 dir = Vector2.Normalize(center - p);
                    p += dir * curve * starburstSize;
                    diamondPoints.Add(p);
                }
                
                // Draw starburst first (behind everything else)
                // Fill the diamond shape using scanline fill (bucket fill approach)
                if (diamondPoints.Count >= 3)
                {
                    // Find bounding box
                    float minY = float.MaxValue;
                    float maxY = float.MinValue;
                    foreach (var point in diamondPoints)
                    {
                        if (point.Y < minY) minY = point.Y;
                        if (point.Y > maxY) maxY = point.Y;
                    }
                    
                    // For each scanline (Y coordinate), find left and right edges
                    for (float y = minY; y <= maxY; y += 1.0f)
                    {
                        List<float> intersections = new List<float>();
                        
                        // Find intersections with each edge
                        for (int i = 0; i < diamondPoints.Count; i++)
                        {
                            int next = (i + 1) % diamondPoints.Count;
                            Vector2 p1 = diamondPoints[i];
                            Vector2 p2 = diamondPoints[next];
                            
                            // Check if scanline intersects this edge
                            if ((p1.Y <= y && p2.Y > y) || (p1.Y > y && p2.Y <= y))
                            {
                                // Calculate intersection X
                                float t = (y - p1.Y) / (p2.Y - p1.Y);
                                float x = p1.X + t * (p2.X - p1.X);
                                intersections.Add(x);
                            }
                        }
                        
                        // Sort intersections and draw horizontal lines between pairs
                        intersections.Sort();
                        for (int i = 0; i < intersections.Count - 1; i += 2)
                        {
                            int x1 = (int)intersections[i];
                            int x2 = (int)intersections[i + 1];
                            Raylib.DrawLine(x1, (int)y, x2, (int)y, fillColor);
                        }
                    }
                }
                
                // Draw curved edges outline (on top of fill)
                for (int i = 0; i < diamondPoints.Count; i++)
                {
                    int next = (i + 1) % diamondPoints.Count;
                    Raylib.DrawLine((int)diamondPoints[i].X, (int)diamondPoints[i].Y, 
                        (int)diamondPoints[next].X, (int)diamondPoints[next].Y, starburstColor);
                }
                
                // Draw glow layers for prominent appearance (on top of starburst)
                // Outer glow (very dim, translucent)
                Color outerGlow = new Color(system.StarColor.R, system.StarColor.G, system.StarColor.B, (byte)(system.StarColor.A * 0.1f));
                Raylib.DrawCircle(screenX, screenY, starRadius + 8, outerGlow);
                
                // Mid glow (translucent)
                Color midGlow = new Color(system.StarColor.R, system.StarColor.G, system.StarColor.B, (byte)(system.StarColor.A * 0.3f));
                Raylib.DrawCircle(screenX, screenY, starRadius + 4, midGlow);
                
                // Core glow (translucent)
                Color coreGlow = new Color(system.StarColor.R, system.StarColor.G, system.StarColor.B, (byte)(system.StarColor.A * 0.5f));
                Raylib.DrawCircle(screenX, screenY, starRadius + 2, coreGlow);
                
                // Main star (translucent)
                Color translucentStarColor = new Color(system.StarColor.R, system.StarColor.G, system.StarColor.B, (byte)(system.StarColor.A * 0.7f));
                Raylib.DrawCircle(screenX, screenY, starRadius, translucentStarColor);
                
                // Bright center (translucent)
                Color brightCenter = new Color((byte)255, (byte)255, (byte)255, (byte)(system.StarColor.A * 0.6f));
                Raylib.DrawCircle(screenX, screenY, starRadius / 2, brightCenter);
                
                // Draw system name if close enough
                float distance = Vector2.Distance(ship.Position, system.Position);
                if (distance < 300)
                {
                    int nameY = screenY - starRadius - 25;
                    if (nameY > 100) // Don't draw name if too close to top UI
                    {
                        Raylib.DrawText(system.Name, screenX - Raylib.MeasureText(system.Name, 16) / 2, nameY, 16, Color.WHITE);
                    }
                }
            }
        }
        
        // Draw ship in the center
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
        
        // Box dimensions - indicator boxes to the left of menu items
        const int indicatorSize = 16;
        const int indicatorSpacing = 8;
        const int innerBoxPadding = 2;
        
        // Draw menu items
        for (int i = 0; i < currentMenuItems.Length; i++)
        {
            bool isFocused = i == selectedMenuIndex;
            bool isActive = IsMenuItemActive(menuLevel, i);
            
            Color itemColor = isFocused ? Color.YELLOW : Color.LIGHTGRAY;
            
            // Calculate indicator box position (to the left of text)
            int indicatorX = panelX + panelPadding;
            int indicatorY = yPos;
            int textX = indicatorX + indicatorSize + indicatorSpacing;
            
            // Draw outer box: always visible, filled when focused
            if (isFocused)
            {
                // Outer box filled when focused
                Color outerBoxColor = new Color(150, 150, 200, 255); // Bright blue-gray
                Raylib.DrawRectangle(indicatorX, indicatorY, indicatorSize, indicatorSize, outerBoxColor);
                // Draw outline on top
                Color outerBoxOutline = new Color(200, 200, 255, 255);
                Raylib.DrawRectangleLines(indicatorX, indicatorY, indicatorSize, indicatorSize, outerBoxOutline);
            }
            else
            {
                // Outer box outline when not focused
                Color outerBoxOutline = new Color(100, 100, 120, 255);
                Raylib.DrawRectangleLines(indicatorX, indicatorY, indicatorSize, indicatorSize, outerBoxOutline);
            }
            
            // Draw inner box: filled when active, outline when focused but not active
            int innerBoxX = indicatorX + innerBoxPadding;
            int innerBoxY = indicatorY + innerBoxPadding;
            int innerBoxSize = indicatorSize - innerBoxPadding * 2;
            
            if (isActive)
            {
                // Inner box filled when active
                Color innerBoxColor = new Color(220, 240, 255, 255); // Very bright
                Raylib.DrawRectangle(innerBoxX, innerBoxY, innerBoxSize, innerBoxSize, innerBoxColor);
            }
            else if (isFocused)
            {
                // When focused but not active: clear inner area (use panel background color) then draw outline
                Color panelBgColor = new Color(30, 30, 35, 255);
                Raylib.DrawRectangle(innerBoxX, innerBoxY, innerBoxSize, innerBoxSize, panelBgColor);
                // Draw inner box outline
                Color innerBoxOutline = new Color(255, 255, 255, 255); // White outline
                Raylib.DrawRectangleLines(innerBoxX, innerBoxY, innerBoxSize, innerBoxSize, innerBoxOutline);
            }
            
            // Draw menu item text (no numbering)
            Raylib.DrawText(currentMenuItems[i], textX, yPos, menuFontSize, itemColor);
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
