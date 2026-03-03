using Raylib_cs;
using System.Numerics;
using System;

namespace StarflightGame;

public class Planet
{
    public string Name { get; set; } = "";
    public Vector2 Position { get; set; }
    public float Radius { get; set; }
    public Color SurfaceColor { get; set; }
    public List<Mineral> Minerals { get; private set; }
    private Random random;

    public Planet(string name, Vector2 position, float radius, Color surfaceColor)
    {
        Name = name;
        Position = position;
        Radius = radius;
        SurfaceColor = surfaceColor;
        Minerals = new List<Mineral>();
        random = new Random(name.GetHashCode());
        
        GenerateMinerals();
    }

    private void GenerateMinerals()
    {
        int mineralCount = random.Next(5, 15);
        string[] mineralNames = { "Iron", "Gold", "Platinum", "Dilithium", "Crystal", "Uranium" };
        Color[] mineralColors = { Color.GRAY, Color.GOLD, Color.WHITE, Color.BLUE, Color.PURPLE, Color.GREEN };

        for (int i = 0; i < mineralCount; i++)
        {
            float angle = (float)(random.NextDouble() * Math.PI * 2);
            float distance = (float)(random.NextDouble() * Radius * 0.8f);
            Vector2 mineralPos = Position + new Vector2(
                (float)Math.Cos(angle) * distance,
                (float)Math.Sin(angle) * distance
            );

            int mineralType = random.Next(mineralNames.Length);
            int value = random.Next(50, 500);
            
            Minerals.Add(new Mineral(
                mineralNames[mineralType],
                value,
                mineralPos,
                mineralColors[mineralType]
            ));
        }
    }

    public void Update(Ship ship)
    {
        if (!ship.CanMove()) return;

        Vector2 movement = Vector2.Zero;
        float speed = ship.Speed;

        if (Raylib.IsKeyDown(KeyboardKey.KEY_W) || Raylib.IsKeyDown(KeyboardKey.KEY_UP))
            movement.Y -= speed;
        if (Raylib.IsKeyDown(KeyboardKey.KEY_S) || Raylib.IsKeyDown(KeyboardKey.KEY_DOWN))
            movement.Y += speed;
        if (Raylib.IsKeyDown(KeyboardKey.KEY_A) || Raylib.IsKeyDown(KeyboardKey.KEY_LEFT))
            movement.X -= speed;
        if (Raylib.IsKeyDown(KeyboardKey.KEY_D) || Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT))
            movement.X += speed;

        if (movement != Vector2.Zero)
        {
            ship.Position += movement;
            ship.ConsumeFuelForMovement();
            
            // Keep ship within planet bounds
            float distanceFromCenter = Vector2.Distance(ship.Position, Position);
            if (distanceFromCenter > Radius - 10)
            {
                Vector2 direction = Vector2.Normalize(ship.Position - Position);
                ship.Position = Position + direction * (Radius - 10);
            }
        }

        // Mining
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_SPACE))
        {
            var mineral = GetMineralAt(ship.Position);
            if (mineral != null && !mineral.Mined)
            {
                mineral.Mined = true;
                ship.AddMinerals(1);
                ship.AddCredits(mineral.Value);
            }
        }
    }

    public Mineral? GetMineralAt(Vector2 position)
    {
        const float miningRange = 30.0f;
        foreach (var mineral in Minerals)
        {
            if (!mineral.Mined && Vector2.Distance(position, mineral.Position) < miningRange)
            {
                return mineral;
            }
        }
        return null;
    }

    public void Draw(int screenWidth, int screenHeight, Ship ship)
    {
        // Draw planet surface (scaled view)
        float scale = Math.Min(screenWidth, screenHeight) / (Radius * 2.5f);
        Vector2 center = new Vector2(screenWidth / 2, screenHeight / 2);
        
        // Draw planet circle
        Raylib.DrawCircleV(center, Radius * scale, SurfaceColor);
        
        // Draw minerals
        foreach (var mineral in Minerals)
        {
            if (!mineral.Mined)
            {
                Vector2 screenPos = center + (mineral.Position - Position) * scale;
                Raylib.DrawCircleV(screenPos, 5, mineral.Color);
            }
        }

        // Draw ship
        Vector2 shipScreenPos = center + (ship.Position - Position) * scale;
        Raylib.DrawCircleV(shipScreenPos, 8, Color.WHITE);
        Raylib.DrawCircleV(shipScreenPos, 6, Color.BLUE);

        // Draw planet name
        Raylib.DrawText(Name, 10, screenHeight - 60, 24, Color.WHITE);
    }
}
