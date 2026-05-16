using System.Numerics;

namespace StarflightGame;

public interface IShip
{
    Vector2 Position { get; set; }
    int Credits { get; set; }
    int Minerals { get; set; }
    IReadOnlyList<CargoHoldEntry> Cargo { get; }
    int CargoCapacity { get; }
    int FuelCapacity { get; }
    float FuelQuantity { get; }
    float GetFuelFillFraction();
    float Speed { get; set; }
    float Rotation { get; set; }
    Vector2 Velocity { get; set; }
    bool ManeuverThrustForward { get; set; }
    bool ManeuverThrustReverse { get; set; }

    void ConsumeFuel(float amount);
    void ConsumeFuelForMovement();
    void RefuelToFull();
    void AddCredits(int amount);
    void AddMinerals(int amount);
    bool CanMove();
    int GetCargoFillPercent();
    float ShieldStrength { get; set; }
    float HullStrength { get; set; }
    float MaxShieldStrength { get; }
    float MaxHullStrength { get; }
    float GetShieldFillFraction();
    float GetHullFillFraction();
    void ResetCombatHealth();
    void ApplyCombatDamage(float amount);
    bool IsCombatDestroyed();
}

public class Ship : IShip
{
    private const float FuelConsumptionRate = 0.05f;
    private const float DefaultMaxShield = 100f;
    private const float DefaultMaxHull = 100f;

    private readonly List<CargoHoldEntry> _cargo;
    private readonly int _fuelCargoIndex;
    private float _fuelUnits;

    public Vector2 Position { get; set; } = Vector2.Zero;
    public int Credits { get; set; } = 1000;
    public int Minerals { get; set; } = 0;
    public IReadOnlyList<CargoHoldEntry> Cargo => _cargo;
    public int CargoCapacity { get; }
    public int FuelCapacity { get; }
    public float FuelQuantity => _fuelUnits;
    public float Speed { get; set; } = 3.0f;
    public float Rotation { get; set; } = -MathF.PI / 2.0f; // Default: pointing up (0 degrees = right, -90 = up)
    public Vector2 Velocity { get; set; } = Vector2.Zero;
    public bool ManeuverThrustForward { get; set; }
    public bool ManeuverThrustReverse { get; set; }
    public float ShieldStrength { get; set; } = DefaultMaxShield;
    public float HullStrength { get; set; } = DefaultMaxHull;
    public float MaxShieldStrength => DefaultMaxShield;
    public float MaxHullStrength => DefaultMaxHull;

    public Ship(IResourceLoader resourceLoader)
    {
        CargoManifest manifest = resourceLoader.LoadCargoManifest();
        CargoCapacity = manifest.Capacity;
        FuelCapacity = manifest.FuelCapacity;

        _cargo = new List<CargoHoldEntry>(manifest.Items.Count);
        _fuelCargoIndex = -1;
        for (int i = 0; i < manifest.Items.Count; i++)
        {
            CargoHoldEntry entry = manifest.Items[i];
            _cargo.Add(new CargoHoldEntry
            {
                Name = entry.Name,
                Quantity = entry.Quantity,
                Category = entry.Category
            });

            if (string.Equals(entry.Name, "Fuel", StringComparison.OrdinalIgnoreCase))
            {
                _fuelCargoIndex = i;
            }
        }

        _fuelUnits = _fuelCargoIndex >= 0 ? _cargo[_fuelCargoIndex].Quantity : 0f;
        SyncFuelCargoEntry();
    }

    public float GetFuelFillFraction()
    {
        if (FuelCapacity <= 0)
        {
            return 0f;
        }

        return Math.Clamp(_fuelUnits / FuelCapacity, 0f, 1f);
    }

    public void ConsumeFuel(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        _fuelUnits = Math.Max(0f, _fuelUnits - amount);
        SyncFuelCargoEntry();
    }

    public void ConsumeFuelForMovement()
    {
        ConsumeFuel(FuelConsumptionRate);
    }

    public void RefuelToFull()
    {
        _fuelUnits = FuelCapacity;
        SyncFuelCargoEntry();
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
        return _fuelUnits > 0.001f;
    }

    public int GetCargoFillPercent()
    {
        float totalUnits = 0f;
        for (int i = 0; i < _cargo.Count; i++)
        {
            if (i == _fuelCargoIndex)
            {
                totalUnits += _fuelUnits;
            }
            else
            {
                totalUnits += _cargo[i].Quantity;
            }
        }

        if (CargoCapacity <= 0)
        {
            return 0;
        }

        return Math.Clamp((int)(totalUnits * 100f / CargoCapacity), 0, 100);
    }

    public float GetShieldFillFraction()
    {
        if (MaxShieldStrength <= 0f)
        {
            return 0f;
        }

        return Math.Clamp(ShieldStrength / MaxShieldStrength, 0f, 1f);
    }

    public float GetHullFillFraction()
    {
        if (MaxHullStrength <= 0f)
        {
            return 0f;
        }

        return Math.Clamp(HullStrength / MaxHullStrength, 0f, 1f);
    }

    public void ResetCombatHealth()
    {
        ShieldStrength = MaxShieldStrength;
        HullStrength = MaxHullStrength;
    }

    public void ApplyCombatDamage(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        if (ShieldStrength > 0f)
        {
            float absorbed = Math.Min(ShieldStrength, amount);
            ShieldStrength -= absorbed;
            amount -= absorbed;
        }

        if (amount > 0f)
        {
            HullStrength = Math.Max(0f, HullStrength - amount);
        }
    }

    public bool IsCombatDestroyed()
    {
        return HullStrength <= 0f;
    }

    private void SyncFuelCargoEntry()
    {
        if (_fuelCargoIndex < 0)
        {
            return;
        }

        _cargo[_fuelCargoIndex].Quantity = (int)MathF.Round(_fuelUnits);
    }
}
