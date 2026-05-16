using System.Numerics;

namespace StarflightGame.Mining;

public sealed class MiningRig
{
    public MiningRig(string systemId, string planetName, Vector3 surfaceDirection)
    {
        SystemId = systemId;
        PlanetName = planetName;
        SurfaceDirection = Vector3.Normalize(surfaceDirection);
        PlacedAtUtc = DateTime.UtcNow;
    }

    public string SystemId { get; }

    public string PlanetName { get; }

    /// <summary>Unit vector on the planet surface in model space (before Y-axis spin).</summary>
    public Vector3 SurfaceDirection { get; }

    public DateTime PlacedAtUtc { get; }
}
