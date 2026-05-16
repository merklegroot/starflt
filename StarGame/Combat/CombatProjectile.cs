using System.Numerics;

namespace StarflightGame.Combat;

internal sealed class CombatProjectile
{
    public Vector2 Position;
    public Vector2 Velocity;
    public bool IsPlayerOwned;
    public float Lifetime;
    public float Damage;
}
