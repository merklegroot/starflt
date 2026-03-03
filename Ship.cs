using Raylib_cs;
using System.Numerics;

namespace StarflightGame;

public class Ship
{
    public Vector2 Position { get; set; } = Vector2.Zero;
    public float Fuel { get; set; } = 100.0f;
    public int Credits { get; set; } = 1000;
    public int Minerals { get; set; } = 0;
    public float Speed { get; set; } = 2.0f;
    
    private const float FuelConsumptionRate = 0.05f;
    private const float MaxFuel = 100.0f;

    public void ConsumeFuel(float amount)
    {
        Fuel = Math.Max(0, Fuel - amount);
    }

    public void ConsumeFuelForMovement()
    {
        ConsumeFuel(FuelConsumptionRate);
    }

    public void Refuel(float amount)
    {
        Fuel = Math.Min(MaxFuel, Fuel + amount);
    }

    public void AddCredits(int amount)
    {
        Credits += amount;
    }

    public void AddMinerals(int amount)
    {
        Minerals += amount;
    }

    public bool CanMove()
    {
        return Fuel > 0;
    }
}
