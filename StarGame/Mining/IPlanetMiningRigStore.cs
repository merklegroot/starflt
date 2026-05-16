namespace StarflightGame.Mining;

public interface IPlanetMiningRigStore
{
    IReadOnlyList<MiningRig> GetRigs(string systemId, string planetName);

    int GetRigCount(string systemId, string planetName);

    bool TryAddRig(MiningRig rig, out string failureReason);

    int GetTotalRigCount();
}
