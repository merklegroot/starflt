using Raylib_cs;
using StarflightGame.Constants;

namespace StarflightGame.Views;

public sealed class RightPanel
{
    private readonly StatusPanel _statusPanel = new StatusPanel();
    private readonly GameMenu _gameMenu = new GameMenu();

    public int MenuLevel => _gameMenu.MenuLevel;

    public void ResetSubmenuToTop()
    {
        _gameMenu.ResetSubmenuToTop();
    }

    public void UpdateNavigation(ref GameState currentState, ref bool justSwitchedState)
    {
        _gameMenu.UpdateNavigation(ref currentState, ref justSwitchedState);
    }

    public void Draw(int screenWidth, int screenHeight, IShip ship, GameState currentState)
    {
        int panelWidth = LayoutConstants.RightPanelWidth;
        int panelX = screenWidth - panelWidth;
        const int menuFontSize = 20;

        Raylib.DrawRectangle(panelX, 0, panelWidth, screenHeight, new Color(30, 30, 35, 255));

        Raylib.DrawLine(panelX, 0, panelX, screenHeight, Color.DARKGRAY);

        int yPos = LayoutConstants.RightPanelPadding;

        yPos = _gameMenu.Draw(panelX, yPos, panelWidth, LayoutConstants.RightPanelPadding, menuFontSize, LayoutConstants.RightPanelLineSpacing, currentState);

        yPos += 10;
        Raylib.DrawLine(panelX + LayoutConstants.RightPanelPadding, yPos, panelX + panelWidth - LayoutConstants.RightPanelPadding, yPos, Color.DARKGRAY);

        yPos += 20;

        yPos = _statusPanel.Draw(panelX, yPos, ship, currentState);
    }
}
