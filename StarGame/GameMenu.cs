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
    private readonly string[] _topMenuItems = { "Planet", "Captain", "Navigator", "Info" };
    private readonly string[] _navigatorSubMenuItems = { "Starmap", "Star system" };
    private readonly string[] _infoSubMenuItems = { "Minerals", "Manifest" };
    private static readonly string[] _noOptionsPlaceholder = { "No options" };

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
        if (currentState == GameState.ShipStatus || currentState == GameState.MineralCatalog || currentState == GameState.ShipManifest || currentState == GameState.Combat)
            return;

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE) && currentState != GameState.StarMap && currentState != GameState.PlanetaryEncounter && currentState != GameState.StarSystemView && currentState != GameState.Combat)
        {
            if (_menuLevel == 3)
            {
                _menuLevel = 0;
                _selectedMenuIndex = 1;
            }
            else if (_menuLevel == 2)
            {
                _menuLevel = 0;
                _selectedMenuIndex = 3;
            }
            else if (_menuLevel == 1)
            {
                _menuLevel = 0;
                _selectedMenuIndex = 2;
            }

            return;
        }

        string[] displayRows = GetDisplayMenuRows();
        bool readOnlyMenu = IsReadOnlyMenuLevel();

        if (currentState != GameState.StarMap && currentState != GameState.StarSystemView && currentState != GameState.Combat && !readOnlyMenu)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_UP))
            {
                _selectedMenuIndex = Math.Max(0, _selectedMenuIndex - 1);
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.KEY_DOWN))
            {
                _selectedMenuIndex = Math.Min(displayRows.Length - 1, _selectedMenuIndex + 1);
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
            else if (Raylib.IsKeyPressed(KeyboardKey.KEY_FOUR))
            {
                _selectedMenuIndex = 3;
            }
        }
        else if (_menuLevel == 1)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_ONE))
            {
                _selectedMenuIndex = 0;
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.KEY_TWO))
            {
                _selectedMenuIndex = 1;
            }
        }
        else if (_menuLevel == 2)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_ONE))
            {
                _selectedMenuIndex = 0;
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.KEY_TWO))
            {
                _selectedMenuIndex = 1;
            }
        }

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ENTER))
        {
            if (readOnlyMenu)
            {
                return;
            }

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
                    _menuLevel = 3;
                    _selectedMenuIndex = 0;
                }
                else if (_selectedMenuIndex == 2)
                {
                    _menuLevel = 1;
                    _selectedMenuIndex = 0;
                }
                else if (_selectedMenuIndex == 3)
                {
                    _menuLevel = 2;
                    _selectedMenuIndex = 0;
                }
            }
            else if (_menuLevel == 1)
            {
                if (_selectedMenuIndex == 0)
                {
                    currentState = GameState.StarMap;
                    justSwitchedState = true;
                    _menuLevel = 0;
                    _selectedMenuIndex = 0;
                }
                else if (_selectedMenuIndex == 1)
                {
                    currentState = GameState.StarSystemView;
                    justSwitchedState = true;
                    _menuLevel = 0;
                    _selectedMenuIndex = 0;
                }
            }
            else if (_menuLevel == 2)
            {
                if (_selectedMenuIndex == 0)
                {
                    currentState = GameState.MineralCatalog;
                    justSwitchedState = true;
                }
                else if (_selectedMenuIndex == 1)
                {
                    currentState = GameState.ShipManifest;
                    justSwitchedState = true;
                }
            }
        }

        _selectedMenuIndex = Math.Clamp(_selectedMenuIndex, 0, Math.Max(0, displayRows.Length - 1));
    }

    public int Draw(int panelX, int yPos, int panelWidth, int panelPadding, int menuFontSize, int lineSpacing, GameState currentState)
    {
        int y = yPos;

        string menuTitle = _menuLevel switch
        {
            0 => "MENU",
            1 => "NAVIGATOR",
            2 => "INFO",
            3 => "CAPTAIN",
            _ => "MENU"
        };
        UiText.DrawText(menuTitle, panelX + panelPadding, y, menuFontSize, Color.WHITE);
        y += menuFontSize + 15;

        string[] rawItems = GetCurrentMenuItemsRaw();
        string[] displayRows = GetDisplayMenuRows();
        bool readOnlyMenu = rawItems.Length == 0;

        const int indicatorSize = 16;
        const int indicatorSpacing = 8;
        const int innerBoxPadding = 2;

        for (int i = 0; i < displayRows.Length; i++)
        {
            bool isFocused = !readOnlyMenu && i == _selectedMenuIndex;
            bool isActive = !readOnlyMenu && IsMenuItemActive(_menuLevel, i, currentState);

            Color itemColor = readOnlyMenu ? Color.DARKGRAY : isFocused ? Color.YELLOW : Color.LIGHTGRAY;

            int indicatorX = panelX + panelPadding;
            int indicatorY = y;
            int textX = indicatorX + indicatorSize + indicatorSpacing;

            if (readOnlyMenu)
            {
                Color dimOutline = new Color(70, 70, 85, 255);
                Raylib.DrawRectangleLines(indicatorX, indicatorY, indicatorSize, indicatorSize, dimOutline);
            }
            else if (isFocused)
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

            UiText.DrawText(displayRows[i], textX, y, menuFontSize, itemColor);
            y += lineSpacing;
        }

        y += 10;
        if (_menuLevel > 0)
        {
            UiText.DrawText("ESC: Back", panelX + panelPadding, y, menuFontSize - 4, Color.DARKGRAY);
        }
        else
        {
            UiText.DrawText("ENTER: Select", panelX + panelPadding, y, menuFontSize - 4, Color.DARKGRAY);
        }

        return y;
    }

    private bool IsReadOnlyMenuLevel()
    {
        return GetCurrentMenuItemsRaw().Length == 0;
    }

    private string[] GetCurrentMenuItemsRaw()
    {
        if (_menuLevel == 0)
            return _topMenuItems;

        if (_menuLevel == 1)
            return _navigatorSubMenuItems;

        if (_menuLevel == 2)
            return _infoSubMenuItems;

        if (_menuLevel == 3)
            return Array.Empty<string>();

        return _topMenuItems;
    }

    private string[] GetDisplayMenuRows()
    {
        string[] raw = GetCurrentMenuItemsRaw();
        if (raw.Length == 0)
            return _noOptionsPlaceholder;

        return raw;
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
                return currentState == GameState.StarMap;
            }

            if (index == 1)
            {
                return currentState == GameState.StarSystemView;
            }
        }
        else if (level == 2)
        {
            if (index == 0)
            {
                return currentState == GameState.MineralCatalog;
            }

            if (index == 1)
            {
                return currentState == GameState.ShipManifest;
            }
        }

        return false;
    }
}
