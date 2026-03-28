using Raylib_cs;

namespace StarflightGame;

public interface IGameMenu
{
    int MenuLevel { get; }

    void ResetSubmenuToTop();

    void UpdateNavigation(ref GameState currentState, ref bool justSwitchedState);

    int Draw(int panelX, int yPos, int panelWidth, int panelPadding, int menuFontSize, int lineSpacing, GameState currentState);
}


public sealed class GameMenu : IGameMenu
{
    private readonly string[] _topMenuItems = { "Planet", "Captain", "Navigator" };
    private readonly string[] _navigatorSubMenuItems = { "Manuever", "Starmap", "Star system" };

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

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE) && currentState != GameState.StarMap && currentState != GameState.Maneuver && currentState != GameState.PlanetaryEncounter && currentState != GameState.StarSystemView)
        {
            if (_menuLevel > 0)
            {
                _menuLevel = 0;
                _selectedMenuIndex = 0;
            }
            return;
        }

        string[] currentMenuItems = _menuLevel == 0 ? _topMenuItems : _navigatorSubMenuItems;

        if (currentState != GameState.StarMap && currentState != GameState.Maneuver && currentState != GameState.StarSystemView)
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

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ENTER))
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
                else if (_selectedMenuIndex == 2)
                {
                    currentState = GameState.StarSystemView;
                    justSwitchedState = true;
                }
            }
        }

        _selectedMenuIndex = Math.Clamp(_selectedMenuIndex, 0, currentMenuItems.Length - 1);
    }

    public int Draw(int panelX, int yPos, int panelWidth, int panelPadding, int menuFontSize, int lineSpacing, GameState currentState)
    {
        int y = yPos;

        string menuTitle = _menuLevel == 0 ? "MENU" : "NAVIGATOR";
        Raylib.DrawText(menuTitle, panelX + panelPadding, y, menuFontSize, Color.WHITE);
        y += menuFontSize + 15;

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
            int indicatorY = y;
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

            Raylib.DrawText(currentMenuItems[i], textX, y, menuFontSize, itemColor);
            y += lineSpacing;
        }

        y += 10;
        if (_menuLevel > 0)
        {
            Raylib.DrawText("ESC: Back", panelX + panelPadding, y, menuFontSize - 4, Color.DARKGRAY);
        }
        else
        {
            Raylib.DrawText("ENTER: Select", panelX + panelPadding, y, menuFontSize - 4, Color.DARKGRAY);
        }

        return y;
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
            if (index == 2)
            {
                return currentState == GameState.StarSystemView;
            }
        }

        return false;
    }
}
