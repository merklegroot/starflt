using System.Numerics;

namespace StarflightGame.Combat;

internal sealed class CombatExplosionBurst
{
    public Vector2 Position = Vector2.Zero;
    public float FlashTimer;
    public List<CombatExplosionParticle> Particles = new List<CombatExplosionParticle>();

    public bool IsFinished => FlashTimer <= 0f && Particles.Count == 0;
}
