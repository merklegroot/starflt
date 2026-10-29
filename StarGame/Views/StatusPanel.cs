using Raylib_cs;
using StarflightGame;
using System.Numerics;
using StarflightGame.Constants;

namespace StarflightGame.Views;

public interface IStatusPanel
{
    int Draw(
        int panelX,
        int yPos,
        int panelWidth,
        IShip ship,
        GameState currentState,
        Vector2? starSystemLocalPosition = null,
        PlanetSensorScan? sensorScan = null);
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

    public int Draw(
        int panelX,
        int yPos,
        int panelWidth,
        IShip ship,
        GameState currentState,
        Vector2? starSystemLocalPosition = null,
        PlanetSensorScan? sensorScan = null)
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

        if (sensorScan != null)
        {
            DrawSensorContents(contentLeft, contentRight, contentWidth, y, sensorScan);
            return boxTop + boxHeight + 8;
        }

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
        float fuelFillFraction = ship.GetFuelFillFraction();
        float shieldFillFraction = ship.GetShieldFillFraction();
        float hullFillFraction = ship.GetHullFillFraction();
        bool inCombat = currentState == GameState.Combat;
        string damageLabel = inCombat
            ? (hullFillFraction > 0.66f ? "NONE" : hullFillFraction > 0.33f ? "MOD" : "HEAVY")
            : (fuelFillFraction > 0.12f ? "NONE" : "LOW");
        int cargoPct = ship.GetCargoFillPercent();
        string fuelStr = ship.FuelQuantity.ToString("F1");
        string shieldsStr = inCombat
            ? (shieldFillFraction > 0f ? $"{ship.ShieldStrength:F0}" : "DOWN")
            : (fuelFillFraction > 0.20f ? "UP" : "DOWN");
        string weaponsStr = inCombat ? "PHASERS" : "UNARMED";

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

        float shieldFill = inCombat ? shieldFillFraction : (fuelFillFraction > 0.15f ? 1f : 0.35f);
        float fuelFill = inCombat ? hullFillFraction : fuelFillFraction;
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
        DrawRightColumnRow(rightColX, contentRight, ref ry, "FUEL :", fuelStr, LayoutConstants.StatusPanelFontSize);
        DrawRightColumnRow(rightColX, contentRight, ref ry, "SHIELDS :", shieldsStr, LayoutConstants.StatusPanelFontSize);
        DrawRightColumnRow(rightColX, contentRight, ref ry, "WEAP :", weaponsStr, LayoutConstants.StatusPanelFontSize);

        int gaugeBlockBottom = gaugeRowTop + GaugeBarH + 18;
        y = Math.Max(gaugeBlockBottom, ry) + 8;

        Raylib.DrawLine(contentLeft, y, contentRight, y, new Color(60, 80, 140, 255));
        y += 12;

        bool useStarSystemSpeed = currentState == GameState.CanopyView
            || currentState == GameState.StarSystemView
            || currentState == GameState.Combat;
        float actualSpeed = useStarSystemSpeed ? ship.Velocity.Length() : 0f;
        DrawLabelValueRow(ref y, "CREDITS :", ship.Credits.ToString("N0"), LayoutConstants.StatusPanelFontSize);
        DrawLabelValueRow(ref y, "SPEED :", $"{actualSpeed:F1}", LayoutConstants.StatusPanelFontSize);

        Vector2 posForDisplay = starSystemLocalPosition ?? ship.Position;
        DrawLabelValueRow(ref y, "POS X :", $"{posForDisplay.X:F1}", LayoutConstants.StatusPanelFontSize);
        DrawLabelValueRow(ref y, "POS Y :", $"{posForDisplay.Y:F1}", LayoutConstants.StatusPanelFontSize);

        return boxTop + boxHeight + 8;
    }

    private static void DrawSensorContents(int contentLeft, int contentRight, int contentWidth, int y, PlanetSensorScan scan)
    {
        int fontSize = LayoutConstants.StatusPanelFontSize;
        float progress = scan.ScanProgress;

        void DrawLabelValueRow(ref int rowY, string label, string value, int size)
        {
            UiText.DrawText(label, contentLeft, rowY, size, _labelCyan);
            int vw = UiText.MeasureText(value, size);
            UiText.DrawText(value, contentRight - vw, rowY, size, _valueLavender);
            rowY += RowSpacing;
        }

        UiText.DrawTextCenteredAtX("SENSORS", contentLeft + contentWidth * 0.5f, y, HeaderFontSize, Color.WHITE);
        y += HeaderFontSize + 6;

        int planetRadius = Math.Min(contentWidth / 2 - 6, 72);
        int cx = contentLeft + contentWidth / 2;
        int cy = y + planetRadius;
        DrawSimplifiedScanPlanet(cx, cy, planetRadius, scan, progress);
        y = cy + planetRadius + 8;

        int legendSize = fontSize - 4;
        Raylib.DrawCircle(contentLeft + 5, y + 8, 4, new Color(70, 230, 90, 255));
        UiText.DrawText("LIFE", contentLeft + 14, y, legendSize, _labelCyan);
        Raylib.DrawCircle(contentLeft + 78, y + 8, 4, new Color(240, 200, 70, 255));
        UiText.DrawText("MINERAL", contentLeft + 88, y, legendSize, _labelCyan);
        y += 20;

        int shownBio = (int)MathF.Round(scan.BioPercent * progress);
        int shownMin = (int)MathF.Round(scan.MineralPercent * progress);
        DrawLabelValueRow(ref y, "MASS :", scan.MassLabel, fontSize);
        DrawLabelValueRow(ref y, "BIO :", $"{shownBio} %", fontSize);
        DrawLabelValueRow(ref y, "MIN :", $"{shownMin} %", fontSize);
    }

    private static void DrawSimplifiedScanPlanet(int cx, int cy, int radius, PlanetSensorScan scan, float progress)
    {
        Color baseColor = scan.DiscColor.A == 0 ? new Color(80, 110, 160, 255) : scan.DiscColor;
        Color dark = Shade(baseColor, 0.45f);
        Color mid = Shade(baseColor, 0.75f);

        Raylib.DrawCircle(cx, cy, radius + 2, new Color(20, 40, 80, 255));
        Raylib.DrawCircle(cx, cy, radius, dark);
        Raylib.DrawCircle(cx - radius / 6, cy - radius / 7, (int)(radius * 0.82f), mid);
        Raylib.DrawCircle(cx - radius / 4, cy - radius / 5, (int)(radius * 0.45f), baseColor);

        DrawTerrainBlobs(cx, cy, radius, scan, baseColor);

        float sweepX = cx - radius + progress * radius * 2f;
        if (progress < 1f)
        {
            int x0 = cx - radius;
            int clipW = Math.Clamp((int)(sweepX - x0), 0, radius * 2);
            Raylib.DrawRectangle(x0, cy - radius, clipW, 2, new Color(80, 180, 255, 40));
            DrawScanLine(cx, cy, radius, sweepX);
        }

        IReadOnlyList<SensorContact> contacts = scan.Contacts;
        for (int i = 0; i < contacts.Count; i++)
        {
            SensorContact contact = contacts[i];
            float revealAt = (contact.X + 1f) * 0.5f;
            if (progress < revealAt)
            {
                continue;
            }

            int px = cx + (int)(contact.X * radius);
            int py = cy + (int)(contact.Y * radius);
            int dx = px - cx;
            int dy = py - cy;
            if (dx * dx + dy * dy > (radius - 3) * (radius - 3))
            {
                continue;
            }

            float age = Math.Clamp((progress - revealAt) / 0.12f, 0f, 1f);
            int size = contact.Kind == SensorContactKind.Life ? 3 : 2;
            Color c = contact.Color;
            byte a = (byte)(80 + 175 * age);
            Raylib.DrawCircle(px, py, size + 1, new Color(c.R, c.G, c.B, (byte)(a / 3)));
            Raylib.DrawCircle(px, py, size, new Color(c.R, c.G, c.B, a));
        }

        Raylib.DrawCircleLines(cx, cy, radius, new Color(140, 200, 255, 180));
    }

    private static void DrawTerrainBlobs(int cx, int cy, int radius, PlanetSensorScan scan, Color baseColor)
    {
        if (scan.TerrainStyle is PlanetTerrainStyle.GasBanded or PlanetTerrainStyle.IceBanded)
        {
            Color band = new Color(baseColor.R, baseColor.G, baseColor.B, (byte)70);
            Raylib.DrawEllipse(cx, cy - radius / 5, (int)(radius * 0.85f), radius / 10, band);
            Raylib.DrawEllipse(cx, cy + radius / 6, (int)(radius * 0.9f), radius / 12, band);
            return;
        }

        Color land = scan.TerrainStyle switch
        {
            PlanetTerrainStyle.Oceanic => new Color(40, 130, 55, 180),
            PlanetTerrainStyle.Lush => new Color(30, 120, 45, 200),
            PlanetTerrainStyle.Desert => new Color(180, 110, 50, 160),
            PlanetTerrainStyle.Ice => new Color(220, 230, 240, 160),
            PlanetTerrainStyle.Volcanic => new Color(90, 30, 20, 180),
            PlanetTerrainStyle.Rocky => new Color(90, 90, 95, 160),
            PlanetTerrainStyle.Clouded => new Color(230, 210, 150, 140),
            _ => new Color(70, 90, 70, 140)
        };

        Raylib.DrawCircle(cx - radius / 4, cy + radius / 8, radius / 4, land);
        Raylib.DrawCircle(cx + radius / 5, cy - radius / 6, radius / 6, land);
        Raylib.DrawCircle(cx + radius / 8, cy + radius / 3, radius / 7, land);
    }

    private static void DrawScanLine(int cx, int cy, int radius, float sweepX)
    {
        float dx = sweepX - cx;
        float inner = radius * radius - dx * dx;
        if (inner <= 0f)
        {
            return;
        }

        float half = MathF.Sqrt(inner);
        int x = (int)sweepX;
        Raylib.DrawLine(x, (int)(cy - half), x, (int)(cy + half), new Color(180, 255, 255, 220));
        Raylib.DrawLine(x - 1, (int)(cy - half), x - 1, (int)(cy + half), new Color(80, 180, 255, 80));
    }

    private static Color Shade(Color color, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        return new Color(
            (byte)(color.R * amount),
            (byte)(color.G * amount),
            (byte)(color.B * amount),
            (byte)255);
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
