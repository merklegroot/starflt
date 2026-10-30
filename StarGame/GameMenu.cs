using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace StarflightGame;

public interface IGameMenu
{
    int MenuLevel { get; }

    bool IsSelectingLandingSite { get; }

    void ResetSubmenuToTop();

    void EnterLandSubmenu();

    void ShowCaptainMenu();

    void UpdateNavigation(ref GameState currentState, ref bool justSwitchedState);

    bool ConsumeSensorsSelection();

    bool ConsumeLandSelection();

    bool ConsumeLaunchSelection();

    bool ConsumeDisembarkSelection();

    bool ConsumeEmbarkSelection();

    bool ConsumeDescendSelection();

    bool SurfaceCrewDisembarked { get; set; }

    bool ConsumeAbortLandingSelection();

    int Draw(int panelX, int yPos, int panelWidth, int panelPadding, int menuFontSize, int lineSpacing, GameState currentState);
}


public sealed class GameMenu : IGameMenu
{
    private readonly string[] _topMenuItems = { "Planet", "Captain", "Navigator", "Info", "Science Officer" };
    private readonly string[] _navigatorSubMenuItems = { "Starmap", "Star system" };
    private readonly string[] _infoSubMenuItems = { "Minerals", "Manifest" };
    private readonly string[] _captainSubMenuItems = { "Land" };
    private readonly string[] _captainSurfaceSubMenuItems = { "Launch", "Disembark" };
    private readonly string[] _captainEmbarkSubMenuItems = { "Embark" };
    private readonly string[] _landSubMenuItems = { "Select Site", "Descend", "Abort" };
    private readonly string[] _scienceOfficerSubMenuItems = { "Sensors" };
    private static readonly string[] _noOptionsPlaceholder = { "No options" };

    private int _selectedMenuIndex = 0;
    private int _menuLevel = 0;
    private bool _sensorsRequested = false;
    private bool _landRequested = false;
    private bool _launchRequested = false;
    private bool _disembarkRequested = false;
    private bool _embarkRequested = false;
    private bool _descendRequested = false;
    private bool _abortLandingRequested = false;
    private bool _selectingLandingSite = false;
    private readonly List<Rectangle> _menuItemHitRects = new List<Rectangle>();

    public int MenuLevel => _menuLevel;

    public bool IsSelectingLandingSite => _selectingLandingSite;

    public bool SurfaceCrewDisembarked { get; set; }

    public void ResetSubmenuToTop()
    {
        _menuLevel = 0;
        _selectedMenuIndex = 0;
        _selectingLandingSite = false;
    }

    public void EnterLandSubmenu()
    {
        _menuLevel = 5;
        _selectedMenuIndex = 0;
        _selectingLandingSite = false;
    }

    public void ShowCaptainMenu()
    {
        _menuLevel = 3;
        _selectedMenuIndex = 0;
        _selectingLandingSite = false;
    }

    public void UpdateNavigation(ref GameState currentState, ref bool justSwitchedState)
    {
        if (currentState == GameState.ShipStatus || currentState == GameState.MineralCatalog || currentState == GameState.ShipManifest || currentState == GameState.Combat)
            return;

        bool planetSubmenuOpen =
            (currentState == GameState.PlanetaryEncounter || currentState == GameState.PlanetaryExploration)
            && _menuLevel > 0;
        if (InputManager.IsBackPressed()
            && (planetSubmenuOpen
                || (currentState != GameState.StarMap
                    && currentState != GameState.PlanetaryEncounter
                    && currentState != GameState.PlanetaryExploration
                    && currentState != GameState.StarSystemView
                    && currentState != GameState.Combat)))
        {
            if (_menuLevel == 5)
            {
                if (_selectingLandingSite)
                {
                    _selectingLandingSite = false;
                    return;
                }

                _abortLandingRequested = true;
                _menuLevel = 3;
                _selectedMenuIndex = 0;
            }
            else if (_menuLevel == 4)
            {
                _menuLevel = 0;
                _selectedMenuIndex = 4;
            }
            else if (_menuLevel == 3)
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

        string[] displayRows = GetDisplayMenuRows(currentState);
        bool readOnlyMenu = IsReadOnlyMenuLevel(currentState);

        if (currentState != GameState.StarMap
            && currentState != GameState.StarSystemView
            && currentState != GameState.Combat
            && !readOnlyMenu
            && !_selectingLandingSite)
        {
            if (InputManager.IsMenuUpPressed())
            {
                _selectedMenuIndex = Math.Max(0, _selectedMenuIndex - 1);
            }
            else if (InputManager.IsMenuDownPressed())
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
            else if (Raylib.IsKeyPressed(KeyboardKey.KEY_FIVE))
            {
                _selectedMenuIndex = 4;
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
        else if (_menuLevel == 3)
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
        else if (_menuLevel == 4)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_ONE))
            {
                _selectedMenuIndex = 0;
            }
        }
        else if (_menuLevel == 5)
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

        if (InputManager.IsConfirmPressed() && !readOnlyMenu)
        {
            ApplyMenuSelection(ref currentState, ref justSwitchedState);
        }

        HandleMouseInput(ref currentState, ref justSwitchedState, readOnlyMenu);

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
            4 => "SCIENCE OFFICER",
            5 => "LAND",
            _ => "MENU"
        };
        UiText.DrawText(menuTitle, panelX + panelPadding, y, menuFontSize, Color.WHITE);
        y += menuFontSize + 15;

        string[] rawItems = GetCurrentMenuItemsRaw(currentState);
        string[] displayRows = GetDisplayMenuRows(currentState);
        bool readOnlyMenu = rawItems.Length == 0;

        const int indicatorSize = 16;
        const int indicatorSpacing = 8;
        const int innerBoxPadding = 2;

        _menuItemHitRects.Clear();

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

            if (!readOnlyMenu)
            {
                _menuItemHitRects.Add(new Rectangle(panelX, y, panelWidth, lineSpacing));
            }

            UiText.DrawText(displayRows[i], textX, y, menuFontSize, itemColor);
            y += lineSpacing;
        }

        y += 10;
        if (_menuLevel > 0)
        {
            UiText.DrawText("ESC / B: Back", panelX + panelPadding, y, menuFontSize - 4, Color.DARKGRAY);
        }
        else
        {
            UiText.DrawText("ENTER / A: Select  |  D-pad: Navigate", panelX + panelPadding, y, menuFontSize - 4, Color.DARKGRAY);
        }

        return y;
    }

    private void HandleMouseInput(ref GameState currentState, ref bool justSwitchedState, bool readOnlyMenu)
    {
        if (readOnlyMenu || !Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
        {
            return;
        }

        Vector2 mouse = Raylib.GetMousePosition();

        for (int i = 0; i < _menuItemHitRects.Count; i++)
        {
            if (!Raylib.CheckCollisionPointRec(mouse, _menuItemHitRects[i]))
            {
                continue;
            }

            _selectedMenuIndex = i;
            ApplyMenuSelection(ref currentState, ref justSwitchedState);
            return;
        }
    }

    private void ApplyMenuSelection(ref GameState currentState, ref bool justSwitchedState)
    {
        bool isCurrentlyActive = IsMenuItemActive(_menuLevel, _selectedMenuIndex, currentState);
        if (isCurrentlyActive && _menuLevel <= 2)
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
            else if (_selectedMenuIndex == 4)
            {
                _menuLevel = 4;
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
        else if (_menuLevel == 3)
        {
            if (currentState == GameState.PlanetaryExploration)
            {
                if (SurfaceCrewDisembarked)
                {
                    _embarkRequested = true;
                }
                else if (_selectedMenuIndex == 0)
                {
                    _launchRequested = true;
                }
                else if (_selectedMenuIndex == 1)
                {
                    _disembarkRequested = true;
                }
            }
            else if (_selectedMenuIndex == 0)
            {
                _landRequested = true;
            }
        }
        else if (_menuLevel == 4)
        {
            _sensorsRequested = true;
        }
        else if (_menuLevel == 5)
        {
            if (_selectedMenuIndex == 0)
            {
                _selectingLandingSite = !_selectingLandingSite;
            }
            else if (_selectedMenuIndex == 1)
            {
                _selectingLandingSite = false;
                _descendRequested = true;
            }
            else if (_selectedMenuIndex == 2)
            {
                _selectingLandingSite = false;
                _abortLandingRequested = true;
                _menuLevel = 3;
                _selectedMenuIndex = 0;
            }
        }
    }

    public bool ConsumeSensorsSelection()
    {
        bool requested = _sensorsRequested;
        _sensorsRequested = false;
        return requested;
    }

    public bool ConsumeLandSelection()
    {
        bool requested = _landRequested;
        _landRequested = false;
        return requested;
    }

    public bool ConsumeLaunchSelection()
    {
        bool requested = _launchRequested;
        _launchRequested = false;
        return requested;
    }

    public bool ConsumeDisembarkSelection()
    {
        bool requested = _disembarkRequested;
        _disembarkRequested = false;
        return requested;
    }

    public bool ConsumeEmbarkSelection()
    {
        bool requested = _embarkRequested;
        _embarkRequested = false;
        return requested;
    }

    public bool ConsumeDescendSelection()
    {
        bool requested = _descendRequested;
        _descendRequested = false;
        return requested;
    }

    public bool ConsumeAbortLandingSelection()
    {
        bool requested = _abortLandingRequested;
        _abortLandingRequested = false;
        return requested;
    }

    private bool IsReadOnlyMenuLevel(GameState currentState)
    {
        return GetCurrentMenuItemsRaw(currentState).Length == 0;
    }

    private string[] GetCurrentMenuItemsRaw(GameState currentState)
    {
        if (_menuLevel == 0)
            return _topMenuItems;

        if (_menuLevel == 1)
            return _navigatorSubMenuItems;

        if (_menuLevel == 2)
            return _infoSubMenuItems;

        if (_menuLevel == 3)
        {
            if (currentState != GameState.PlanetaryExploration)
            {
                return _captainSubMenuItems;
            }

            return SurfaceCrewDisembarked
                ? _captainEmbarkSubMenuItems
                : _captainSurfaceSubMenuItems;
        }

        if (_menuLevel == 4)
            return _scienceOfficerSubMenuItems;

        if (_menuLevel == 5)
            return _landSubMenuItems;

        return _topMenuItems;
    }

    private string[] GetDisplayMenuRows(GameState currentState)
    {
        string[] raw = GetCurrentMenuItemsRaw(currentState);
        if (raw.Length == 0)
            return _noOptionsPlaceholder;

        return raw;
    }

    private bool IsMenuItemActive(int level, int index, GameState currentState)
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
        else if (level == 5)
        {
            if (index == 0)
            {
                return _selectingLandingSite;
            }
        }

        return false;
    }
}
