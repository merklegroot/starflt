using Raylib_cs;
using StarflightGame.Constants;

namespace StarflightGame.Views;

public interface IStatusPanel
{
    int Draw(int panelX, int yPos, IShip ship, GameState currentState);
}


/// <summary>
/// Renders the ship status block in the right panel: fuel, credits, minerals, speed, and position.
/// Does not own layout of the full panel—callers pass the panel origin and current Y.
/// </summary>
public sealed class StatusPanel : IStatusPanel
{
    public int Draw(int panelX, int yPos, IShip ship, GameState currentState)
    {
        int y = yPos;

        void AddLabeledLine(ref int rowY, int valueOffset, string label, string value, Color valueColor)
        {
            Raylib.DrawText(label, panelX + LayoutConstants.RightPanelPadding, rowY, LayoutConstants.StatusPanelFontSize, Color.WHITE);
            Raylib.DrawText(value, panelX + LayoutConstants.RightPanelPadding + valueOffset, rowY, LayoutConstants.StatusPanelFontSize, valueColor);
            rowY += LayoutConstants.RightPanelLineSpacing;
        }

        void AddVerticalSpacer(ref int rowY, int pixels)
        {
            rowY += pixels;
        }

        void AddLabelLine(ref int rowY, string label)
        {
            Raylib.DrawText(label, panelX + LayoutConstants.RightPanelPadding, rowY, LayoutConstants.StatusPanelFontSize, Color.WHITE);
            rowY += LayoutConstants.RightPanelLineSpacing;
        }

        void AddIndentedLine(ref int rowY, string text, Color color, int advanceAfter)
        {
            int detailFontSize = LayoutConstants.StatusPanelFontSize - 2;
            Raylib.DrawText(text, panelX + LayoutConstants.RightPanelPadding + 10, rowY, detailFontSize, color);
            rowY += advanceAfter;
        }

        Color fuelColor = ship.Fuel > 50 ? Color.GREEN : ship.Fuel > 25 ? Color.YELLOW : Color.RED;
        AddLabeledLine(ref y, 70, "Fuel:", $"{ship.Fuel:F1}%", fuelColor);

        AddLabeledLine(ref y, 80, "Credits:", $"{ship.Credits:N0}", Color.GOLD);

        AddLabeledLine(ref y, 90, "Minerals:", $"{ship.Minerals}", Color.LIGHTGRAY);

        float actualSpeed = currentState == GameState.Maneuver ? ship.Velocity.Length() : 0f;
        AddLabeledLine(ref y, 70, "Speed:", $"{actualSpeed:F1}", Color.SKYBLUE);

        AddVerticalSpacer(ref y, 10);
        AddLabelLine(ref y, "Position:");
        AddIndentedLine(ref y, $"X: {ship.Position.X:F1}", Color.LIGHTGRAY, LayoutConstants.RightPanelLineSpacing - 5);
        AddIndentedLine(ref y, $"Y: {ship.Position.Y:F1}", Color.LIGHTGRAY, 0);

        return y;
    }
}
