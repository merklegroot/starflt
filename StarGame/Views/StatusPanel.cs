using Raylib_cs;
using StarflightGame.Constants;

namespace StarflightGame.Views;

public sealed class StatusPanel
{
    public int Draw(int panelX, int yPos, IShip ship, GameState currentState)
    {
        int y = yPos;

        Raylib.DrawText("Fuel:", panelX + LayoutConstants.RightPanelPadding, y, LayoutConstants.StatusPanelFontSize, Color.WHITE);
        Color fuelColor = ship.Fuel > 50 ? Color.GREEN : ship.Fuel > 25 ? Color.YELLOW : Color.RED;
        Raylib.DrawText($"{ship.Fuel:F1}%", panelX + LayoutConstants.RightPanelPadding + 70, y, LayoutConstants.StatusPanelFontSize, fuelColor);
        y += LayoutConstants.RightPanelLineSpacing;

        Raylib.DrawText("Credits:", panelX + LayoutConstants.RightPanelPadding, y, LayoutConstants.StatusPanelFontSize, Color.WHITE);
        Raylib.DrawText($"{ship.Credits:N0}", panelX + LayoutConstants.RightPanelPadding + 80, y, LayoutConstants.StatusPanelFontSize, Color.GOLD);
        y += LayoutConstants.RightPanelLineSpacing;

        Raylib.DrawText("Minerals:", panelX + LayoutConstants.RightPanelPadding, y, LayoutConstants.StatusPanelFontSize, Color.WHITE);
        Raylib.DrawText($"{ship.Minerals}", panelX + LayoutConstants.RightPanelPadding + 90, y, LayoutConstants.StatusPanelFontSize, Color.LIGHTGRAY);
        y += LayoutConstants.RightPanelLineSpacing;

        float actualSpeed = currentState == GameState.Maneuver ? ship.Velocity.Length() : 0f;
        Raylib.DrawText("Speed:", panelX + LayoutConstants.RightPanelPadding, y, LayoutConstants.StatusPanelFontSize, Color.WHITE);
        Raylib.DrawText($"{actualSpeed:F1}", panelX + LayoutConstants.RightPanelPadding + 70, y, LayoutConstants.StatusPanelFontSize, Color.SKYBLUE);
        y += LayoutConstants.RightPanelLineSpacing;

        y += 10;
        Raylib.DrawText("Position:", panelX + LayoutConstants.RightPanelPadding, y, LayoutConstants.StatusPanelFontSize, Color.WHITE);
        y += LayoutConstants.RightPanelLineSpacing;
        Raylib.DrawText($"X: {ship.Position.X:F1}", panelX + LayoutConstants.RightPanelPadding + 10, y, LayoutConstants.StatusPanelFontSize - 2, Color.LIGHTGRAY);
        y += LayoutConstants.RightPanelLineSpacing - 5;
        Raylib.DrawText($"Y: {ship.Position.Y:F1}", panelX + LayoutConstants.RightPanelPadding + 10, y, LayoutConstants.StatusPanelFontSize - 2, Color.LIGHTGRAY);

        return y;
    }
}
