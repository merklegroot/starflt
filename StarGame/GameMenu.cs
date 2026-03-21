using Raylib_cs;

namespace StarflightGame;

public sealed class GameMenu
{
    private readonly string[] _topMenuItems = { "Planet", "Captain", "Navigator" };
    private readonly string[] _navigatorSubMenuItems = { "Manuever", "Starmap" };

    private int _selectedMenuIndex = 0;
    private int _menuLevel = 0;

    public int MenuLevel => _menuLevel;

    public void ResetSubmenuToTop()
    {
        _menuLevel = 0;
        _selectedMenuIndex = 0;
    }

    public void UpdateNavigation(ref GameState currentState, ref bool justSwitchedState)
    {
        if (currentState == GameState.ShipStatus)
            return;

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE) && currentState != GameState.StarMap && currentState != GameState.Maneuver && currentState != GameState.PlanetaryEncounter)
        {
            if (_menuLevel > 0)
            {
                _menuLevel = 0;
                _selectedMenuIndex = 0;
            }
            return;
        }

        string[] currentMenuItems = _menuLevel == 0 ? _topMenuItems : _navigatorSubMenuItems;

        if (currentState != GameState.StarMap && currentState != GameState.Maneuver)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_UP))
            {
                _selectedMenuIndex = Math.Max(0, _selectedMenuIndex - 1);
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.KEY_DOWN))
            {
                _selectedMenuIndex = Math.Min(currentMenuItems.Length - 1, _selectedMenuIndex + 1);
            }
        }

        if (_menuLevel == 0)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_ONE))
            {
                _selectedMenuIndex = 0;
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.KEY_TWO))
            {
                _selectedMenuIndex = 1;
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.KEY_THREE))
            {
                _selectedMenuIndex = 2;
            }
        }

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_SPACE) || Raylib.IsKeyPressed(KeyboardKey.KEY_ENTER))
        {
            bool isCurrentlyActive = IsMenuItemActive(_menuLevel, _selectedMenuIndex, currentState);
            if (isCurrentlyActive)
            {
                currentState = GameState.CanopyView;
                justSwitchedState = true;
                return;
            }

            if (_menuLevel == 0)
            {
                if (_selectedMenuIndex == 0)
                {
                    currentState = GameState.PlanetaryEncounter;
                    justSwitchedState = true;
                }
                else if (_selectedMenuIndex == 1)
                {
                    // Captain — TODO
                }
                else if (_selectedMenuIndex == 2)
                {
                    _menuLevel = 1;
                    _selectedMenuIndex = 0;
                }
            }
            else if (_menuLevel == 1)
            {
                if (_selectedMenuIndex == 0)
                {
                    currentState = GameState.Maneuver;
                    justSwitchedState = true;
                }
                else if (_selectedMenuIndex == 1)
                {
                    currentState = GameState.StarMap;
                    justSwitchedState = true;
                }
            }
        }

        _selectedMenuIndex = Math.Clamp(_selectedMenuIndex, 0, currentMenuItems.Length - 1);
    }

    public void DrawRightPanel(int screenWidth, int screenHeight, Ship ship, GameState currentState)
    {
        int panelWidth = LayoutConstants.RightPanelWidth;
        int panelX = screenWidth - panelWidth;
        const int panelPadding = 15;
        const int textFontSize = 18;
        const int menuFontSize = 20;
        const int lineSpacing = 25;

        Raylib.DrawRectangle(panelX, 0, panelWidth, screenHeight, new Color(30, 30, 35, 255));

        Raylib.DrawLine(panelX, 0, panelX, screenHeight, Color.DARKGRAY);

        int yPos = panelPadding;

        DrawMenu(panelX, ref yPos, panelWidth, panelPadding, menuFontSize, lineSpacing, currentState);

        yPos += 10;
        Raylib.DrawLine(panelX + panelPadding, yPos, panelX + panelWidth - panelPadding, yPos, Color.DARKGRAY);

        yPos += 20;

        Raylib.DrawText("Fuel:", panelX + panelPadding, yPos, textFontSize, Color.WHITE);
        Color fuelColor = ship.Fuel > 50 ? Color.GREEN : ship.Fuel > 25 ? Color.YELLOW : Color.RED;
        Raylib.DrawText($"{ship.Fuel:F1}%", panelX + panelPadding + 70, yPos, textFontSize, fuelColor);
        yPos += lineSpacing;

        Raylib.DrawText("Credits:", panelX + panelPadding, yPos, textFontSize, Color.WHITE);
        Raylib.DrawText($"{ship.Credits:N0}", panelX + panelPadding + 80, yPos, textFontSize, Color.GOLD);
        yPos += lineSpacing;

        Raylib.DrawText("Minerals:", panelX + panelPadding, yPos, textFontSize, Color.WHITE);
        Raylib.DrawText($"{ship.Minerals}", panelX + panelPadding + 90, yPos, textFontSize, Color.LIGHTGRAY);
        yPos += lineSpacing;

        float actualSpeed = currentState == GameState.Maneuver ? ship.Velocity.Length() : 0f;
        Raylib.DrawText("Speed:", panelX + panelPadding, yPos, textFontSize, Color.WHITE);
        Raylib.DrawText($"{actualSpeed:F1}", panelX + panelPadding + 70, yPos, textFontSize, Color.SKYBLUE);
        yPos += lineSpacing;

        yPos += 10;
        Raylib.DrawText("Position:", panelX + panelPadding, yPos, textFontSize, Color.WHITE);
        yPos += lineSpacing;
        Raylib.DrawText($"X: {ship.Position.X:F1}", panelX + panelPadding + 10, yPos, textFontSize - 2, Color.LIGHTGRAY);
        yPos += lineSpacing - 5;
        Raylib.DrawText($"Y: {ship.Position.Y:F1}", panelX + panelPadding + 10, yPos, textFontSize - 2, Color.LIGHTGRAY);
    }

    private void DrawMenu(int panelX, ref int yPos, int panelWidth, int panelPadding, int menuFontSize, int lineSpacing, GameState currentState)
    {
        string menuTitle = _menuLevel == 0 ? "MENU" : "NAVIGATOR";
        Raylib.DrawText(menuTitle, panelX + panelPadding, yPos, menuFontSize, Color.WHITE);
        yPos += menuFontSize + 15;

        string[] currentMenuItems = _menuLevel == 0 ? _topMenuItems : _navigatorSubMenuItems;

        const int indicatorSize = 16;
        const int indicatorSpacing = 8;
        const int innerBoxPadding = 2;

        for (int i = 0; i < currentMenuItems.Length; i++)
        {
            bool isFocused = i == _selectedMenuIndex;
            bool isActive = IsMenuItemActive(_menuLevel, i, currentState);

            Color itemColor = isFocused ? Color.YELLOW : Color.LIGHTGRAY;

            int indicatorX = panelX + panelPadding;
            int indicatorY = yPos;
            int textX = indicatorX + indicatorSize + indicatorSpacing;

            if (isFocused)
            {
                Color outerBoxColor = new Color(150, 150, 200, 255);
                Raylib.DrawRectangle(indicatorX, indicatorY, indicatorSize, indicatorSize, outerBoxColor);
                Color outerBoxOutline = new Color(200, 200, 255, 255);
                Raylib.DrawRectangleLines(indicatorX, indicatorY, indicatorSize, indicatorSize, outerBoxOutline);
            }
            else
            {
                Color outerBoxOutline = new Color(100, 100, 120, 255);
                Raylib.DrawRectangleLines(indicatorX, indicatorY, indicatorSize, indicatorSize, outerBoxOutline);
            }

            int innerBoxX = indicatorX + innerBoxPadding;
            int innerBoxY = indicatorY + innerBoxPadding;
            int innerBoxSize = indicatorSize - innerBoxPadding * 2;

            if (isActive)
            {
                Color innerBoxColor = new Color(220, 240, 255, 255);
                Raylib.DrawRectangle(innerBoxX, innerBoxY, innerBoxSize, innerBoxSize, innerBoxColor);
            }
            else if (isFocused)
            {
                Color panelBgColor = new Color(30, 30, 35, 255);
                Raylib.DrawRectangle(innerBoxX, innerBoxY, innerBoxSize, innerBoxSize, panelBgColor);
                Color innerBoxOutline = new Color(255, 255, 255, 255);
                Raylib.DrawRectangleLines(innerBoxX, innerBoxY, innerBoxSize, innerBoxSize, innerBoxOutline);
            }

            Raylib.DrawText(currentMenuItems[i], textX, yPos, menuFontSize, itemColor);
            yPos += lineSpacing;
        }

        yPos += 10;
        if (_menuLevel > 0)
        {
            Raylib.DrawText("ESC: Back", panelX + panelPadding, yPos, menuFontSize - 4, Color.DARKGRAY);
        }
        else
        {
            Raylib.DrawText("SPACE/ENTER: Select", panelX + panelPadding, yPos, menuFontSize - 4, Color.DARKGRAY);
        }
    }

    private static bool IsMenuItemActive(int level, int index, GameState currentState)
    {
        if (level == 0)
        {
            if (index == 0)
            {
                return currentState == GameState.PlanetaryEncounter;
            }
        }
        else if (level == 1)
        {
            if (index == 0)
            {
                return currentState == GameState.Maneuver;
            }
            if (index == 1)
            {
                return currentState == GameState.StarMap;
            }
        }

        return false;
    }
}
