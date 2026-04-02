using Raylib_cs;

namespace StarflightGame;

public struct LoadedPlanet
{
    public string Name;
    public bool IsFiction;
    public Color SurfaceColor;
    public float SemiMajorAxisAu;
    public float Eccentricity;
    public float ArgumentOfPeriapsisRad;
    public float RadiusKm;
    public PlanetRingData? Rings;
}
