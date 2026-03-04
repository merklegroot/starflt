using Raylib_cs;
using System.Numerics;

namespace StarflightGame;

public enum GameState
{
    CanopyView,
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
    
    // Canopy view - starfield
    private List<Vector2> starfield = new List<Vector2>();
    private Random starfieldRandom = new Random();

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
        
        // Initialize starfield for canopy view
        GenerateStarfield();
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

    private void UpdateStarMap()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ENTER) && currentSystem != null && currentSystem.Planets.Count > 0)
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
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE))
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
        
        // Navigate menu with arrow keys
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_UP))
        {
            selectedMenuIndex = Math.Max(0, selectedMenuIndex - 1);
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.KEY_DOWN))
        {
            selectedMenuIndex = Math.Min(currentMenuItems.Length - 1, selectedMenuIndex + 1);
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
                    // TODO: Handle Manuever
                }
                else if (selectedMenuIndex == 1) // Starmap
                {
                    // Switch to StarMap view
                    currentState = GameState.StarMap;
                    menuLevel = 0;
                    selectedMenuIndex = 0;
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

    private void GenerateStarfield()
    {
        starfield.Clear();
        int starCount = 200;
        const int panelWidth = 250; // Match the panel width
        
        for (int i = 0; i < starCount; i++)
        {
            starfield.Add(new Vector2(
                starfieldRandom.Next(0, screenWidth - panelWidth),
                starfieldRandom.Next(0, screenHeight)
            ));
        }
    }

    private void DrawCanopyView()
    {
        // Draw starfield background
        foreach (var star in starfield)
        {
            // Draw stars as small white dots
            Raylib.DrawPixel((int)star.X, (int)star.Y, Color.WHITE);
        }
        
        // Draw some brighter stars (every 10th star)
        for (int i = 0; i < starfield.Count; i += 10)
        {
            var star = starfield[i];
            Raylib.DrawCircle((int)star.X, (int)star.Y, 1, Color.WHITE);
        }
        
        // Draw canopy frame/border to simulate ship window
        int frameThickness = 20;
        Color frameColor = new Color(40, 40, 45, 255);
        
        // Top frame
        Raylib.DrawRectangle(0, 0, screenWidth - 250, frameThickness, frameColor);
        // Bottom frame
        Raylib.DrawRectangle(0, screenHeight - frameThickness, screenWidth - 250, frameThickness, frameColor);
        // Left frame
        Raylib.DrawRectangle(0, 0, frameThickness, screenHeight, frameColor);
        // Right frame (before panel)
        Raylib.DrawRectangle(screenWidth - 250 - frameThickness, 0, frameThickness, screenHeight, frameColor);
        
        // Draw status text
        Raylib.DrawText("CANOPY VIEW", 30, 30, 24, Color.WHITE);
        Raylib.DrawText("Engines: OFF", 30, 60, 18, Color.RED);
        Raylib.DrawText("Use Navigator menu to access Starmap", 30, screenHeight - 50, 16, Color.YELLOW);
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
            
            // Draw menu item
            string prefix = menuLevel == 0 ? $"{i + 1}. " : "  ";
            Raylib.DrawText($"{prefix}{currentMenuItems[i]}", panelX + panelPadding, yPos, menuFontSize, itemColor);
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
}
