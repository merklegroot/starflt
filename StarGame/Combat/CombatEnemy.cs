using System.Numerics;

namespace StarflightGame.Combat;

internal sealed class CombatEnemy
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Rotation;
    public float Hull;
    public float FireCooldown;
    public bool IsAlive => Hull > 0f;
}
