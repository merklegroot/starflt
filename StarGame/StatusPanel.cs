using Raylib_cs;

namespace StarflightGame;

public sealed class StatusPanel
{
    public int Draw(int panelX, int yPos, int panelPadding, int textFontSize, int lineSpacing, Ship ship, GameState currentState)
    {
        int y = yPos;

        Raylib.DrawText("Fuel:", panelX + panelPadding, y, textFontSize, Color.WHITE);
        Color fuelColor = ship.Fuel > 50 ? Color.GREEN : ship.Fuel > 25 ? Color.YELLOW : Color.RED;
        Raylib.DrawText($"{ship.Fuel:F1}%", panelX + panelPadding + 70, y, textFontSize, fuelColor);
        y += lineSpacing;

        Raylib.DrawText("Credits:", panelX + panelPadding, y, textFontSize, Color.WHITE);
        Raylib.DrawText($"{ship.Credits:N0}", panelX + panelPadding + 80, y, textFontSize, Color.GOLD);
        y += lineSpacing;

        Raylib.DrawText("Minerals:", panelX + panelPadding, y, textFontSize, Color.WHITE);
        Raylib.DrawText($"{ship.Minerals}", panelX + panelPadding + 90, y, textFontSize, Color.LIGHTGRAY);
        y += lineSpacing;

        float actualSpeed = currentState == GameState.Maneuver ? ship.Velocity.Length() : 0f;
        Raylib.DrawText("Speed:", panelX + panelPadding, y, textFontSize, Color.WHITE);
        Raylib.DrawText($"{actualSpeed:F1}", panelX + panelPadding + 70, y, textFontSize, Color.SKYBLUE);
        y += lineSpacing;

        y += 10;
        Raylib.DrawText("Position:", panelX + panelPadding, y, textFontSize, Color.WHITE);
        y += lineSpacing;
        Raylib.DrawText($"X: {ship.Position.X:F1}", panelX + panelPadding + 10, y, textFontSize - 2, Color.LIGHTGRAY);
        y += lineSpacing - 5;
        Raylib.DrawText($"Y: {ship.Position.Y:F1}", panelX + panelPadding + 10, y, textFontSize - 2, Color.LIGHTGRAY);

        return y;
    }
}
