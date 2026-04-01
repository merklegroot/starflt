using Raylib_cs;

namespace StarflightGame;

/// <summary>
/// Planetary ring system from catalog data (e.g. planets.json).
/// </summary>
public struct PlanetRingData
{
    public float InnerRadiusKm;
    public float OuterRadiusKm;
    public float ThicknessKm;
    public float Opacity;
    public Color RingColor;
    public string ParticleTexture;
    public bool HasGaps;

    public readonly bool IsValid => InnerRadiusKm > 0f
        && OuterRadiusKm > InnerRadiusKm
        && Opacity > 0f;
}
