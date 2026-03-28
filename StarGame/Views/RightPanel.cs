using Raylib_cs;
using System.Numerics;
using StarflightGame.Constants;

namespace StarflightGame.Views;

public interface IRightPanel
{
    int MenuLevel { get; }

    void ResetSubmenuToTop();

    void UpdateNavigation(ref GameState currentState, ref bool justSwitchedState);

    void Draw(int screenWidth, int screenHeight, IShip ship, GameState currentState, Vector2? starSystemLocalPosition = null);
}


/// <summary>
/// Right-hand chrome: dark sidebar with <see cref="IGameMenu"/> navigation and <see cref="StatusPanel"/> ship readouts.
/// </summary>
public sealed class RightPanel : IRightPanel
{
    private readonly IStatusPanel _statusPanel;
    private readonly IGameMenu _gameMenu;

    public RightPanel(IStatusPanel statusPanel, IGameMenu gameMenu)
    {
        _statusPanel = statusPanel;
        _gameMenu = gameMenu;
    }

    public int MenuLevel => _gameMenu.MenuLevel;

    public void ResetSubmenuToTop()
    {
        _gameMenu.ResetSubmenuToTop();
    }

    public void UpdateNavigation(ref GameState currentState, ref bool justSwitchedState)
    {
        _gameMenu.UpdateNavigation(ref currentState, ref justSwitchedState);
    }

    public void Draw(int screenWidth, int screenHeight, IShip ship, GameState currentState, Vector2? starSystemLocalPosition = null)
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

        yPos = _statusPanel.Draw(panelX, yPos, ship, currentState, starSystemLocalPosition);
    }
}
