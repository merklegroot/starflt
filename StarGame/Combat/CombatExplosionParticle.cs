using Raylib_cs;
using System.Numerics;

namespace StarflightGame.Combat;

internal sealed class CombatExplosionParticle
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Lifetime;
    public float MaxLifetime;
    public float Size;
    public Color Color;
    public bool IsAlive => Lifetime > 0f;
}
