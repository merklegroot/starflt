namespace StarflightGame.Mining;

public interface IPlanetMiningRigStore
{
    int GetRigCount(string systemId, string planetName);

    int GetStoredMinerals(string systemId, string planetName);

    bool TryAddRig(string systemId, string planetName, out string failureReason);

    bool TryHarvest(string systemId, string planetName, out int harvested, out string failureReason);

    void UpdateProduction(float deltaTime);
}
