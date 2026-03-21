using Raylib_cs;
using System.Numerics;

namespace StarflightGame;

public static class ShipRenderer
{
    public static void Draw(int centerX, int centerY, float rotation, bool forwardThrust = false, bool reverseThrust = false)
    {
        Vector2 center = new Vector2(centerX, centerY);

        Vector2[] baseShipPoints = new Vector2[]
        {
            new Vector2(centerX, centerY - 30),
            new Vector2(centerX - 25, centerY + 20),
            new Vector2(centerX + 25, centerY + 20)
        };

        Vector2[] shipPoints = new Vector2[]
        {
            RotatePoint(baseShipPoints[0], center, rotation),
            RotatePoint(baseShipPoints[1], center, rotation),
            RotatePoint(baseShipPoints[2], center, rotation)
        };

        Raylib.DrawTriangle(shipPoints[0], shipPoints[1], shipPoints[2], Color.GRAY);
        Raylib.DrawTriangleLines(shipPoints[0], shipPoints[1], shipPoints[2], Color.WHITE);

        Vector2[] baseCockpitPoints = new Vector2[]
        {
            new Vector2(centerX, centerY - 15),
            new Vector2(centerX - 8, centerY - 5),
            new Vector2(centerX + 8, centerY - 5)
        };

        Vector2[] cockpitPoints = new Vector2[]
        {
            RotatePoint(baseCockpitPoints[0], center, rotation),
            RotatePoint(baseCockpitPoints[1], center, rotation),
            RotatePoint(baseCockpitPoints[2], center, rotation)
        };
        Raylib.DrawTriangle(cockpitPoints[0], cockpitPoints[1], cockpitPoints[2], new Color(50, 100, 150, 200));

        Vector2[] enginePositions = new Vector2[]
        {
            new Vector2(centerX - 20, centerY + 20),
            new Vector2(centerX + 12, centerY + 20)
        };

        foreach (var enginePos in enginePositions)
        {
            Vector2 rotatedPos = RotatePoint(enginePos, center, rotation);
            Color engineBody = forwardThrust ? new Color(70, 70, 85, 255) : Color.DARKGRAY;
            Raylib.DrawRectangle((int)rotatedPos.X - 4, (int)rotatedPos.Y - 6, 8, 12, engineBody);
        }

        if (forwardThrust)
        {
            Vector2 aft = RotatePoint(new Vector2(centerX, centerY + 28), center, rotation);
            Vector2 backward = new Vector2(-MathF.Sin(rotation), MathF.Cos(rotation));
            Vector2 flameTip = aft + backward * 18f;
            Raylib.DrawLine((int)aft.X, (int)aft.Y, (int)flameTip.X, (int)flameTip.Y, new Color(255, 180, 80, 220));
            Raylib.DrawLine((int)(aft.X - 3), (int)aft.Y, (int)(flameTip.X - 2), (int)(flameTip.Y + 2), new Color(255, 220, 120, 180));
            Raylib.DrawLine((int)(aft.X + 3), (int)aft.Y, (int)(flameTip.X + 2), (int)(flameTip.Y + 2), new Color(255, 220, 120, 180));
        }
        else if (reverseThrust)
        {
            Vector2 aft = RotatePoint(new Vector2(centerX, centerY + 28), center, rotation);
            Vector2 forwardDir = new Vector2(MathF.Sin(rotation), -MathF.Cos(rotation));
            Vector2 puff = aft + forwardDir * 10f;
            Raylib.DrawLine((int)aft.X, (int)aft.Y, (int)puff.X, (int)puff.Y, new Color(150, 200, 255, 160));
        }

        Vector2 lineStart = RotatePoint(new Vector2(centerX - 15, centerY + 5), center, rotation);
        Vector2 lineEnd = RotatePoint(new Vector2(centerX + 15, centerY + 5), center, rotation);
        Raylib.DrawLine((int)lineStart.X, (int)lineStart.Y, (int)lineEnd.X, (int)lineEnd.Y, Color.DARKGRAY);
    }

    private static Vector2 RotatePoint(Vector2 point, Vector2 center, float angle)
    {
        float cos = MathF.Cos(angle);
        float sin = MathF.Sin(angle);
        float dx = point.X - center.X;
        float dy = point.Y - center.Y;
        return new Vector2(
            center.X + dx * cos - dy * sin,
            center.Y + dx * sin + dy * cos);
    }
}
