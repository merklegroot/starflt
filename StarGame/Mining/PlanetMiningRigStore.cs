namespace StarflightGame.Mining;

public sealed class PlanetMiningRigStore : IPlanetMiningRigStore
{
    private const int MaxRigsPerPlanet = 12;

    private const float MinSurfaceSeparationRadians = 0.18f;

    private readonly List<MiningRig> _rigs = new List<MiningRig>();

    public IReadOnlyList<MiningRig> GetRigs(string systemId, string planetName)
    {
        string key = BuildKey(systemId, planetName);
        var matches = new List<MiningRig>();
        for (int i = 0; i < _rigs.Count; i++)
        {
            MiningRig rig = _rigs[i];
            if (string.Equals(BuildKey(rig.SystemId, rig.PlanetName), key, StringComparison.Ordinal))
            {
                matches.Add(rig);
            }
        }

        return matches;
    }

    public int GetRigCount(string systemId, string planetName) => GetRigs(systemId, planetName).Count;

    public bool TryAddRig(MiningRig rig, out string failureReason)
    {
        if (string.IsNullOrWhiteSpace(rig.SystemId) || string.IsNullOrWhiteSpace(rig.PlanetName))
        {
            failureReason = "Unknown planet.";
            return false;
        }

        IReadOnlyList<MiningRig> existing = GetRigs(rig.SystemId, rig.PlanetName);
        if (existing.Count >= MaxRigsPerPlanet)
        {
            failureReason = $"This planet already has {MaxRigsPerPlanet} mining rigs.";
            return false;
        }

        for (int i = 0; i < existing.Count; i++)
        {
            float dot = System.Numerics.Vector3.Dot(existing[i].SurfaceDirection, rig.SurfaceDirection);
            dot = Math.Clamp(dot, -1f, 1f);
            float angle = MathF.Acos(dot);
            if (angle < MinSurfaceSeparationRadians)
            {
                failureReason = "Too close to an existing rig.";
                return false;
            }
        }

        _rigs.Add(rig);
        failureReason = "";
        return true;
    }

    public int GetTotalRigCount() => _rigs.Count;

    private static string BuildKey(string systemId, string planetName) =>
        systemId.Trim().ToLowerInvariant() + "|" + planetName.Trim().ToLowerInvariant();
}
