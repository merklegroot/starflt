using System.Numerics;

namespace StarflightGame;

public interface IShip
{
    Vector2 Position { get; set; }
    float Fuel { get; set; }
    int Credits { get; set; }
    int Minerals { get; set; }
    float Speed { get; set; }
    float Rotation { get; set; }
    Vector2 Velocity { get; set; }
    bool ManeuverThrustForward { get; set; }
    bool ManeuverThrustReverse { get; set; }

    void ConsumeFuel(float amount);
    void ConsumeFuelForMovement();
    void Refuel(float amount);
    void AddCredits(int amount);
    void AddMinerals(int amount);
    bool CanMove();
}

public class Ship : IShip
{
    public Vector2 Position { get; set; } = Vector2.Zero;
    public float Fuel { get; set; } = 100.0f;
    public int Credits { get; set; } = 1000;
    public int Minerals { get; set; } = 0;
    public float Speed { get; set; } = 3.0f;
    public float Rotation { get; set; } = -MathF.PI / 2.0f; // Default: pointing up (0 degrees = right, -90 = up)
    public Vector2 Velocity { get; set; } = Vector2.Zero;
    public bool ManeuverThrustForward { get; set; }
    public bool ManeuverThrustReverse { get; set; }
    
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
