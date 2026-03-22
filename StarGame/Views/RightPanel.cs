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
        const int panelPadding = 15;
        const int textFontSize = 18;
        const int menuFontSize = 20;
        const int lineSpacing = 25;

        Raylib.DrawRectangle(panelX, 0, panelWidth, screenHeight, new Color(30, 30, 35, 255));

        Raylib.DrawLine(panelX, 0, panelX, screenHeight, Color.DARKGRAY);

        int yPos = panelPadding;

        yPos = _gameMenu.Draw(panelX, yPos, panelWidth, panelPadding, menuFontSize, lineSpacing, currentState);

        yPos += 10;
        Raylib.DrawLine(panelX + panelPadding, yPos, panelX + panelWidth - panelPadding, yPos, Color.DARKGRAY);

        yPos += 20;

        yPos = _statusPanel.Draw(panelX, yPos, panelPadding, textFontSize, lineSpacing, ship, currentState);
    }
}
