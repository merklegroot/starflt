using Raylib_cs;
using System.Numerics;

namespace StarflightGame;

public enum GameState
{
    StarMap,
    PlanetaryExploration,
    ShipStatus
}

public class Game
{
    private readonly int screenWidth;
    private readonly int screenHeight;
    private GameState currentState = GameState.StarMap;
    
    private StarSystem? currentSystem;
    private Planet? currentPlanet;
    private Ship ship;
    private StarMap starMap;
    public bool ShouldExit { get; private set; } = false;

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
    }

    public void Update()
    {
        // Check for quit key (X) in any state
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_X))
        {
            ShouldExit = true;
            return;
        }

        switch (currentState)
        {
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
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE))
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

    public void Draw()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.BLACK);

        switch (currentState)
        {
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
        const int titleFontSize = 24;
        const int textFontSize = 18;
        const int lineSpacing = 25;

        // Draw panel background
        Raylib.DrawRectangle(panelX, 0, panelWidth, screenHeight, new Color(30, 30, 35, 255));
        
        // Draw panel border
        Raylib.DrawLine(panelX, 0, panelX, screenHeight, Color.DARKGRAY);
        
        // Draw title
        int yPos = panelPadding;
        Raylib.DrawText("SHIP STATUS", panelX + panelPadding, yPos, titleFontSize, Color.WHITE);
        
        // Draw separator line
        yPos += titleFontSize + 10;
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
}
