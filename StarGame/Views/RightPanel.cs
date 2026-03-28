using Raylib_cs;
using System.Numerics;
using StarflightGame;
using StarflightGame.Constants;

namespace StarflightGame.Views;

public interface IRightPanel
{
    int MenuLevel { get; }

    void ResetSubmenuToTop();

    void UpdateNavigation(ref GameState currentState, ref bool justSwitchedState);

    void Draw(
        int screenWidth,
        int screenHeight,
        IShip ship,
        GameState currentState,
        Vector2? starSystemLocalPosition = null,
        StarSystem? starSystemForPanelMap = null,
        int? starSystemMainViewWidth = null,
        int? starSystemMainViewHeight = null);
}


/// <summary>
/// Right-hand chrome: dark sidebar with <see cref="IGameMenu"/> navigation and <see cref="StatusPanel"/> ship readouts.
/// </summary>
public sealed class RightPanel : IRightPanel
{
    private readonly IStatusPanel _statusPanel;
    private readonly IGameMenu _gameMenu;
    private readonly IStarSystemInteriorView _starSystemInteriorView;

    public RightPanel(IStatusPanel statusPanel, IGameMenu gameMenu, IStarSystemInteriorView starSystemInteriorView)
    {
        _statusPanel = statusPanel;
        _gameMenu = gameMenu;
        _starSystemInteriorView = starSystemInteriorView;
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

    public void Draw(
        int screenWidth,
        int screenHeight,
        IShip ship,
        GameState currentState,
        Vector2? starSystemLocalPosition = null,
        StarSystem? starSystemForPanelMap = null,
        int? starSystemMainViewWidth = null,
        int? starSystemMainViewHeight = null)
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

        if (currentState == GameState.StarSystemView
            && starSystemForPanelMap != null
            && starSystemMainViewWidth.HasValue
            && starSystemMainViewHeight.HasValue)
        {
            int mapHeight = LayoutConstants.StarSystemRightPanelMapHeight;
            int mapWidth = panelWidth - 2 * LayoutConstants.RightPanelPadding;
            Vector2 shipPos = starSystemLocalPosition ?? Vector2.Zero;
            _starSystemInteriorView.DrawOverviewMap(
                panelX + LayoutConstants.RightPanelPadding,
                yPos,
                mapWidth,
                mapHeight,
                starSystemForPanelMap,
                shipPos,
                starSystemMainViewWidth.Value,
                starSystemMainViewHeight.Value);

            yPos += mapHeight + 12;
            Raylib.DrawLine(panelX + LayoutConstants.RightPanelPadding, yPos, panelX + panelWidth - LayoutConstants.RightPanelPadding, yPos, Color.DARKGRAY);
            yPos += 18;
        }

        yPos = _statusPanel.Draw(panelX, yPos, ship, currentState, starSystemLocalPosition);
    }
}
