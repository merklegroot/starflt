using Raylib_cs;
using StarflightGame;
using System.Numerics;
using StarflightGame.Constants;

namespace StarflightGame.Views;

public interface IStatusPanel
{
    int Draw(int panelX, int yPos, int panelWidth, IShip ship, GameState currentState, Vector2? starSystemLocalPosition = null);
}


/// <summary>
/// Renders the ship status block in the right panel, styled after the original Starflight STATUS pane
/// (blue border, black interior, cyan labels, lavender values, vertical S/A gauges with ship icon).
/// Uses the same font as the rest of the UI (<see cref="UiText"/>).
/// </summary>
public sealed class StatusPanel : IStatusPanel
{
    private static readonly Color _borderBlue = new Color(0, 95, 255, 255);
    private static readonly Color _panelBlack = new Color(0, 0, 0, 255);
    private static readonly Color _labelCyan = new Color(0, 235, 255, 255);
    private static readonly Color _valueLavender = new Color(185, 185, 255, 255);
    private static readonly Color _shipIconCyan = new Color(0, 255, 255, 255);

    private const int BorderThickness = 3;
    private const int StatusInnerPad = 8;
    private const int HeaderFontSize = 20;
    private const int RowSpacing = 20;
    private const int GaugeBarW = 14;
    private const int GaugeBarH = 52;
    private const int GaugeClusterGap = 6;

    /// <summary>Total height of the framed STATUS block (content + padding + border).</summary>
    private const int StatusBoxHeight = 300;

    public int Draw(int panelX, int yPos, int panelWidth, IShip ship, GameState currentState, Vector2? starSystemLocalPosition = null)
    {
        int innerLeft = panelX + LayoutConstants.RightPanelPadding;
        int innerWidth = panelWidth - 2 * LayoutConstants.RightPanelPadding;
        int boxLeft = innerLeft;
        int boxTop = yPos;
        int boxWidth = innerWidth;
        int boxHeight = StatusBoxHeight;

        int contentLeft = boxLeft + BorderThickness + StatusInnerPad;
        int contentRight = boxLeft + boxWidth - BorderThickness - StatusInnerPad;
        int contentWidth = contentRight - contentLeft;

        DrawThickRectBorder(boxLeft, boxTop, boxWidth, boxHeight, BorderThickness, _borderBlue);
        Raylib.DrawRectangle(
            boxLeft + BorderThickness,
            boxTop + BorderThickness,
            boxWidth - 2 * BorderThickness,
            boxHeight - 2 * BorderThickness,
            _panelBlack);

        int y = boxTop + BorderThickness + StatusInnerPad;

        void DrawLabelValueRow(ref int rowY, string label, string value, int fontSize)
        {
            UiText.DrawText(label, contentLeft, rowY, fontSize, _labelCyan);
            int vw = UiText.MeasureText(value, fontSize);
            UiText.DrawText(value, contentRight - vw, rowY, fontSize, _valueLavender);
            rowY += RowSpacing;
        }

        void DrawRightColumnRow(int labelX, int valueRight, ref int rowY, string label, string value, int fontSize)
        {
            UiText.DrawText(label, labelX, rowY, fontSize, _labelCyan);
            int vw = UiText.MeasureText(value, fontSize);
            UiText.DrawText(value, valueRight - vw, rowY, fontSize, _valueLavender);
            rowY += RowSpacing;
        }

        string stardate = FormatStardate(ship.Position);
        string damageLabel = ship.Fuel > 12f ? "NONE" : "LOW";
        int cargoPct = Math.Clamp(ship.Minerals * 5, 0, 100);
        string energyStr = (ship.Fuel * 1.911f).ToString("F1");
        string shieldsStr = ship.Fuel > 20f ? "UP" : "DOWN";

        UiText.DrawTextCenteredAtX("STATUS", contentLeft + contentWidth * 0.5f, y, HeaderFontSize, Color.WHITE);
        y += HeaderFontSize + 10;

        DrawLabelValueRow(ref y, "DATE :", stardate, LayoutConstants.StatusPanelFontSize);
        DrawLabelValueRow(ref y, "DAMAGE :", damageLabel, LayoutConstants.StatusPanelFontSize);

        int gaugeRowTop = y;
        int gaugeX = contentLeft;
        int iconCx = gaugeX + GaugeBarW + GaugeClusterGap + 10;
        int rightBarX = iconCx + 12 + GaugeClusterGap;
        int rightColX = rightBarX + GaugeBarW + 10;
        if (rightColX + 72 > contentRight)
        {
            rightColX = Math.Max(gaugeX + 70, contentRight - 72);
        }

        float shieldFill = ship.Fuel > 15f ? 1f : 0.35f;
        float fuelFill = Math.Clamp(ship.Fuel / 100f, 0f, 1f);
        DrawVerticalGauge(gaugeX, gaugeRowTop, GaugeBarW, GaugeBarH, shieldFill, Color.RED);
        int smallLabel = LayoutConstants.StatusPanelFontSize - 2;
        int sW = UiText.MeasureText("S", smallLabel);
        UiText.DrawText("S", gaugeX + (GaugeBarW - sW) / 2, gaugeRowTop + GaugeBarH + 2, smallLabel, Color.WHITE);

        DrawMiniShipIcon(iconCx, gaugeRowTop + GaugeBarH / 2 - 2);

        DrawVerticalGauge(rightBarX, gaugeRowTop, GaugeBarW, GaugeBarH, fuelFill, Color.YELLOW);
        int aW = UiText.MeasureText("A", smallLabel);
        UiText.DrawText("A", rightBarX + (GaugeBarW - aW) / 2, gaugeRowTop + GaugeBarH + 2, smallLabel, Color.WHITE);

        int ry = gaugeRowTop;
        DrawRightColumnRow(rightColX, contentRight, ref ry, "CARGO :", $"{cargoPct} %", LayoutConstants.StatusPanelFontSize);
        DrawRightColumnRow(rightColX, contentRight, ref ry, "ENERGY :", energyStr, LayoutConstants.StatusPanelFontSize);
        DrawRightColumnRow(rightColX, contentRight, ref ry, "SHIELDS :", shieldsStr, LayoutConstants.StatusPanelFontSize);
        DrawRightColumnRow(rightColX, contentRight, ref ry, "WEAP :", "UNARMED", LayoutConstants.StatusPanelFontSize);

        int gaugeBlockBottom = gaugeRowTop + GaugeBarH + 18;
        y = Math.Max(gaugeBlockBottom, ry) + 8;

        Raylib.DrawLine(contentLeft, y, contentRight, y, new Color(60, 80, 140, 255));
        y += 12;

        bool useStarSystemSpeed = currentState == GameState.CanopyView || currentState == GameState.StarSystemView;
        float actualSpeed = useStarSystemSpeed ? ship.Velocity.Length() : 0f;
        DrawLabelValueRow(ref y, "CREDITS :", ship.Credits.ToString("N0"), LayoutConstants.StatusPanelFontSize);
        DrawLabelValueRow(ref y, "SPEED :", $"{actualSpeed:F1}", LayoutConstants.StatusPanelFontSize);

        Vector2 posForDisplay = starSystemLocalPosition ?? ship.Position;
        DrawLabelValueRow(ref y, "POS X :", $"{posForDisplay.X:F1}", LayoutConstants.StatusPanelFontSize);
        DrawLabelValueRow(ref y, "POS Y :", $"{posForDisplay.Y:F1}", LayoutConstants.StatusPanelFontSize);

        return boxTop + boxHeight + 8;
    }

    private static string FormatStardate(Vector2 position)
    {
        int a = (int)(MathF.Abs(position.X) % 30) + 1;
        int b = (int)(MathF.Abs(position.Y) % 30) + 1;
        int c = (int)((MathF.Abs(position.X) + MathF.Abs(position.Y)) % 90) + 10;
        return $"{a:D2}.{b:D2}-{c:D2}-4620";
    }

    private static void DrawVerticalGauge(int x, int y, int w, int h, float fill01, Color fillColor)
    {
        Raylib.DrawRectangleLines(x, y, w, h, Color.WHITE);
        if (fill01 <= 0f)
        {
            return;
        }

        int innerH = h - 2;
        int innerW = w - 2;
        int fillH = Math.Max(1, (int)(innerH * fill01));
        int fillY = y + h - 1 - fillH;
        Raylib.DrawRectangle(x + 1, fillY, innerW, fillH, fillColor);
    }

    private static void DrawMiniShipIcon(int centerX, int centerY)
    {
        Color c = _shipIconCyan;
        Raylib.DrawRectangle(centerX - 2, centerY - 8, 4, 12, c);
        Raylib.DrawRectangle(centerX - 8, centerY + 2, 16, 4, c);
        Raylib.DrawRectangle(centerX, centerY - 11, 1, 1, Color.WHITE);
    }

    private static void DrawThickRectBorder(int x, int y, int w, int h, int t, Color color)
    {
        Raylib.DrawRectangle(x, y, w, t, color);
        Raylib.DrawRectangle(x, y + h - t, w, t, color);
        Raylib.DrawRectangle(x, y, t, h, color);
        Raylib.DrawRectangle(x + w - t, y, t, h, color);
    }
}
