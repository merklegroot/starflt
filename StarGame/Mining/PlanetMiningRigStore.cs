namespace StarflightGame.Mining;

public sealed class PlanetMiningRigStore : IPlanetMiningRigStore
{
    private const int MaxRigsPerPlanet = 12;

    private const float MineralsPerRigPerSecond = 0.25f;

    private const int MaxStoredMineralsPerRig = 50;

    private readonly Dictionary<string, PlanetMiningSite> _sites = new Dictionary<string, PlanetMiningSite>(StringComparer.Ordinal);

    public int GetRigCount(string systemId, string planetName) => GetOrCreateSite(systemId, planetName).RigCount;

    public int GetStoredMinerals(string systemId, string planetName) =>
        (int)MathF.Floor(GetOrCreateSite(systemId, planetName).StoredMinerals);

    public bool TryAddRig(string systemId, string planetName, out string failureReason)
    {
        if (string.IsNullOrWhiteSpace(systemId) || string.IsNullOrWhiteSpace(planetName))
        {
            failureReason = "Unknown planet.";
            return false;
        }

        PlanetMiningSite site = GetOrCreateSite(systemId, planetName);
        if (site.RigCount >= MaxRigsPerPlanet)
        {
            failureReason = $"This planet already has {MaxRigsPerPlanet} mining rigs.";
            return false;
        }

        site.RigCount++;
        failureReason = "";
        return true;
    }

    public bool TryHarvest(string systemId, string planetName, out int harvested, out string failureReason)
    {
        harvested = 0;
        if (string.IsNullOrWhiteSpace(systemId) || string.IsNullOrWhiteSpace(planetName))
        {
            failureReason = "Unknown planet.";
            return false;
        }

        PlanetMiningSite site = GetOrCreateSite(systemId, planetName);
        harvested = (int)MathF.Floor(site.StoredMinerals);
        if (harvested <= 0)
        {
            failureReason = "No minerals ready to harvest.";
            return false;
        }

        site.StoredMinerals -= harvested;
        failureReason = "";
        return true;
    }

    public void UpdateProduction(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        foreach (PlanetMiningSite site in _sites.Values)
        {
            if (site.RigCount <= 0)
            {
                continue;
            }

            float capacity = site.RigCount * MaxStoredMineralsPerRig;
            float produced = site.RigCount * MineralsPerRigPerSecond * deltaTime;
            site.StoredMinerals = MathF.Min(site.StoredMinerals + produced, capacity);
        }
    }

    private PlanetMiningSite GetOrCreateSite(string systemId, string planetName)
    {
        string key = BuildKey(systemId, planetName);
        if (!_sites.TryGetValue(key, out PlanetMiningSite? site))
        {
            site = new PlanetMiningSite(systemId, planetName);
            _sites[key] = site;
        }

        return site;
    }

    private static string BuildKey(string systemId, string planetName) =>
        systemId.Trim().ToLowerInvariant() + "|" + planetName.Trim().ToLowerInvariant();

    private sealed class PlanetMiningSite
    {
        public PlanetMiningSite(string systemId, string planetName)
        {
            SystemId = systemId;
            PlanetName = planetName;
        }

        public string SystemId { get; }

        public string PlanetName { get; }

        public int RigCount { get; set; }

        public float StoredMinerals { get; set; }
    }
}
