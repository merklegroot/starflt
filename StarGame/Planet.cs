using Raylib_cs;
using System.Numerics;
using System;
using System.Collections.Generic;

namespace StarflightGame;

public class Planet
{
    public string Name { get; set; } = "";

    /// <summary>Host star system shown on the encounter HUD. Independent of ship location for random planets.</summary>
    public string SystemName { get; set; } = "";

    public Vector2 Position { get; set; }
    public float Radius { get; set; }
    public Color SurfaceColor { get; set; }

    /// <summary>Equatorial radius in kilometers when known from catalog data; 0 if not specified.</summary>
    public float RadiusKm { get; set; }

    /// <summary>Ring system from catalog data, if any.</summary>
    public PlanetRingData? Rings { get; set; }

    /// <summary>Broad composition from catalog data when the planet was loaded from <c>planets.json</c>.</summary>
    public PlanetComposition Composition { get; set; }

    /// <summary>Visual terrain family used when coloring the procedural sphere.</summary>
    public PlanetTerrainStyle TerrainStyle { get; private set; } = PlanetTerrainStyle.Oceanic;

    /// <summary>Gas and ice giants have no solid surface to set down on.</summary>
    public bool CanLand =>
        Composition != PlanetComposition.GasGiant
        && Composition != PlanetComposition.IceGiant
        && TerrainStyle != PlanetTerrainStyle.GasBanded
        && TerrainStyle != PlanetTerrainStyle.IceBanded;

    private List<Vector3> _spherePoints = new();
    private List<float> _pointHeights = new();
    private List<List<int>> _ringIndices = new();
    private List<TriangleData> _triangles = new();
    private Random _random;
    private Vector3 _noiseOffset;
    private float _minHeight = float.MaxValue;
    private float _maxHeight = float.MinValue;
    private float _bandFrequency = 6.5f;
    private float _bandTurbulence = 1.8f;

    public Planet(
        string name,
        Vector2 position,
        float radius,
        Color surfaceColor,
        float radiusKm = 0f,
        PlanetRingData? rings = null,
        PlanetComposition composition = PlanetComposition.Terrestrial,
        PlanetTerrainStyle? terrainStyle = null)
    {
        Name = name;
        Position = position;
        Radius = radius;
        SurfaceColor = surfaceColor;
        RadiusKm = radiusKm;
        Rings = rings;
        Composition = composition;
        TerrainStyle = terrainStyle ?? PlanetGenerator.ClassifyStyle(composition, surfaceColor);
        ConfigureNoiseForStyle();

        int seed = PlanetGenerator.StableHash(name);
        _random = new Random(seed);
        _noiseOffset = new Vector3(
            _random.Next(100000, 999999),
            _random.Next(100000, 999999),
            _random.Next(100000, 999999));

        GenerateSpherePoints();
        BuildTriangles();
    }

    private struct TriangleData
    {
        public Vector3 V1;
        public Vector3 V2;
        public Vector3 V3;
        public Color Color;
    }

    private void GenerateSpherePoints()
    {
        const int pointsPerRing = 160; // Doubled: was 80
        const int numRings = 120; // Doubled: was 60

        for (int ring = 0; ring < numRings; ring++)
        {
            float theta = (ring / (float)(numRings - 1)) * MathF.PI - MathF.PI / 2f;
            float ringRadius = MathF.Cos(theta);
            int pointsThisRing = (int)(pointsPerRing * MathF.Abs(ringRadius));
            if (pointsThisRing < 3) pointsThisRing = 3;

            // Poles get single point
            if (ring == 0 || ring == numRings - 1) pointsThisRing = 1;

            List<int> ringIndices = new();

            for (int p = 0; p < pointsThisRing; p++)
            {
                float phi = (p / (float)pointsThisRing) * MathF.PI * 2f;

                float x = ringRadius * MathF.Cos(phi);
                float y = MathF.Sin(theta);
                float z = ringRadius * MathF.Sin(phi);

                int idx = _spherePoints.Count;
                _spherePoints.Add(new Vector3(x, y, z));
                _pointHeights.Add(1f);
                ringIndices.Add(idx);
            }

            _ringIndices.Add(ringIndices);
        }

        // fBm heights drive terrain colors only; mesh vertices stay on the unit sphere when building triangles.
        AssignHeightsWith3DNoise();
    }

    private void BuildTriangles()
    {
        // Unit-sphere positions; triangle colors come from height noise (see AssignHeightsWith3DNoise).
        for (int ring = 0; ring < _ringIndices.Count - 1; ring++)
        {
            List<int> currentRing = _ringIndices[ring];
            List<int> nextRing = _ringIndices[ring + 1];

            // Handle poles (single point rings)
            if (currentRing.Count == 1)
            {
                // Connect pole to first ring - full fan coverage
                int poleIdx = currentRing[0];
                Vector3 poleVertex = _spherePoints[poleIdx];
                Color poleColor = GetVertexColor(poleIdx);

                for (int i = 0; i < nextRing.Count; i++)
                {
                    int nextIdx = nextRing[i];
                    int nextIdx2 = nextRing[(i + 1) % nextRing.Count];

                    Vector3 v1 = poleVertex;
                    Vector3 v2 = _spherePoints[nextIdx];
                    Vector3 v3 = _spherePoints[nextIdx2];

                    // Ensure outward-facing winding (counter-clockwise from outside)
                    TriangleData tri = EnsureOutwardFacing(new TriangleData
                    {
                        V1 = v1,
                        V2 = v2,
                        V3 = v3,
                        Color = AverageColor(poleColor, GetVertexColor(nextIdx), GetVertexColor(nextIdx2))
                    });
                    _triangles.Add(tri);
                }
            }
            else if (nextRing.Count == 1)
            {
                // Connect last ring to pole - full fan coverage
                int poleIdx = nextRing[0];
                Vector3 poleVertex = _spherePoints[poleIdx];
                Color poleColor = GetVertexColor(poleIdx);

                for (int i = 0; i < currentRing.Count; i++)
                {
                    int currIdx = currentRing[i];
                    int currIdx2 = currentRing[(i + 1) % currentRing.Count];

                    Vector3 v1 = _spherePoints[currIdx];
                    Vector3 v2 = _spherePoints[currIdx2];
                    Vector3 v3 = poleVertex;

                    // Ensure outward-facing winding
                    TriangleData tri = EnsureOutwardFacing(new TriangleData
                    {
                        V1 = v1,
                        V2 = v2,
                        V3 = v3,
                        Color = AverageColor(GetVertexColor(currIdx), GetVertexColor(currIdx2), poleColor)
                    });
                    _triangles.Add(tri);
                }
            }
            else
            {
                StitchLatitudeRings(currentRing, nextRing);
            }
        }
    }

    /// <summary>
    /// Connects two latitude loops with exactly n1 + n2 triangles and no overlapping faces.
    /// The old dual-fan fill covered the same patch twice; those coplanar triangles z-fought while rotating.
    /// </summary>
    private void StitchLatitudeRings(List<int> currentRing, List<int> nextRing)
    {
        int n1 = currentRing.Count;
        int n2 = nextRing.Count;
        if (n1 == 0 || n2 == 0)
        {
            return;
        }

        int i = 0;
        int j = 0;
        int steps = n1 + n2;

        for (int step = 0; step < steps; step++)
        {
            int currIdx = currentRing[i % n1];
            int nextIdx = nextRing[j % n2];
            bool advanceCurrent = i < n1 && (j >= n2 || (i + 1) * n2 <= (j + 1) * n1);

            if (advanceCurrent)
            {
                int currIdx2 = currentRing[(i + 1) % n1];
                AddSphereTriangle(currIdx, currIdx2, nextIdx);
                i++;
            }
            else
            {
                int nextIdx2 = nextRing[(j + 1) % n2];
                AddSphereTriangle(currIdx, nextIdx2, nextIdx);
                j++;
            }
        }
    }

    private void AddSphereTriangle(int idx1, int idx2, int idx3)
    {
        if (idx1 == idx2 || idx2 == idx3 || idx1 == idx3)
        {
            return;
        }

        Vector3 v1 = _spherePoints[idx1];
        Vector3 v2 = _spherePoints[idx2];
        Vector3 v3 = _spherePoints[idx3];
        Vector3 cross = Vector3.Cross(v2 - v1, v3 - v1);
        if (cross.LengthSquared() < 1e-16f)
        {
            return;
        }

        Color avgColor = AverageColor(
            GetVertexColor(idx1),
            GetVertexColor(idx2),
            GetVertexColor(idx3));
        _triangles.Add(EnsureOutwardFacing(new TriangleData
        {
            V1 = v1,
            V2 = v2,
            V3 = v3,
            Color = avgColor
        }));
    }

    private TriangleData EnsureOutwardFacing(TriangleData tri)
    {
        // Calculate triangle normal
        Vector3 edge1 = tri.V2 - tri.V1;
        Vector3 edge2 = tri.V3 - tri.V1;
        Vector3 normal = Vector3.Cross(edge1, edge2);
        
        // Calculate triangle center
        Vector3 center = (tri.V1 + tri.V2 + tri.V3) / 3.0f;
        
        // For a sphere centered at origin, the normal should point away from origin
        // If dot product of normal with center is negative, triangle faces inward - reverse winding
        if (Vector3.Dot(normal, center) < 0)
        {
            // Reverse winding order
            return new TriangleData { V1 = tri.V1, V2 = tri.V3, V3 = tri.V2, Color = tri.Color };
        }
        
        return tri;
    }

    private Color AverageColor(Color c1, Color c2, Color c3)
    {
        return new Color(
            (byte)((c1.R + c2.R + c3.R) / 3),
            (byte)((c1.G + c2.G + c3.G) / 3),
            (byte)((c1.B + c2.B + c3.B) / 3),
            (byte)255
        );
    }

    private void ConfigureNoiseForStyle()
    {
        switch (TerrainStyle)
        {
            case PlanetTerrainStyle.GasBanded:
                _bandFrequency = 7.2f;
                _bandTurbulence = 2.1f;
                break;
            case PlanetTerrainStyle.IceBanded:
                _bandFrequency = 4.1f;
                _bandTurbulence = 1.15f;
                break;
            default:
                _bandFrequency = 6.5f;
                _bandTurbulence = 1.8f;
                break;
        }
    }

    private void GetNoiseParameters(out float baseScale, out int octaves, out float persistence, out float lacunarity, out bool ridged)
    {
        ridged = false;
        switch (TerrainStyle)
        {
            case PlanetTerrainStyle.Desert:
                baseScale = 2.8f;
                octaves = 6;
                persistence = 0.42f;
                lacunarity = 2.15f;
                break;
            case PlanetTerrainStyle.Rocky:
                baseScale = 3.6f;
                octaves = 7;
                persistence = 0.5f;
                lacunarity = 2.25f;
                ridged = true;
                break;
            case PlanetTerrainStyle.Ice:
                baseScale = 1.7f;
                octaves = 5;
                persistence = 0.4f;
                lacunarity = 2.0f;
                break;
            case PlanetTerrainStyle.Lush:
                baseScale = 2.35f;
                octaves = 6;
                persistence = 0.46f;
                lacunarity = 2.1f;
                break;
            case PlanetTerrainStyle.Clouded:
                baseScale = 1.55f;
                octaves = 6;
                persistence = 0.58f;
                lacunarity = 2.05f;
                break;
            case PlanetTerrainStyle.GasBanded:
                baseScale = 3.8f;
                octaves = 5;
                persistence = 0.52f;
                lacunarity = 2.2f;
                break;
            case PlanetTerrainStyle.IceBanded:
                baseScale = 2.1f;
                octaves = 4;
                persistence = 0.45f;
                lacunarity = 2.0f;
                break;
            case PlanetTerrainStyle.Volcanic:
                baseScale = 2.7f;
                octaves = 6;
                persistence = 0.5f;
                lacunarity = 2.2f;
                ridged = true;
                break;
            default:
                baseScale = 2.2f;
                octaves = 7;
                persistence = 0.48f;
                lacunarity = 2.1f;
                break;
        }
    }

    private void AssignHeightsWith3DNoise()
    {
        GetNoiseParameters(out float baseScale, out int octaves, out float persistence, out float lacunarity, out bool ridged);
        float maxPossible = NoiseAmplitudeSum(octaves, persistence);

        for (int i = 0; i < _spherePoints.Count; i++)
        {
            float height = ComputeRawHeight(_spherePoints[i], baseScale, octaves, persistence, lacunarity, ridged, maxPossible);
            _pointHeights[i] = height;
            _minHeight = Math.Min(_minHeight, height);
            _maxHeight = Math.Max(_maxHeight, height);
        }
    }

    private float ComputeRawHeight(
        Vector3 p,
        float baseScale,
        int octaves,
        float persistence,
        float lacunarity,
        bool ridged,
        float maxPossible)
    {
        float noise = 0f;
        float frequency = baseScale;
        float amp = 1f;

        for (int o = 0; o < octaves; o++)
        {
            float sample = Noise3D.Get(
                (p.X * frequency) + _noiseOffset.X,
                (p.Y * frequency) + _noiseOffset.Y,
                (p.Z * frequency) + _noiseOffset.Z);
            if (ridged)
            {
                sample = 1f - MathF.Abs(sample);
            }

            noise += sample * amp;
            amp *= persistence;
            frequency *= lacunarity;
        }

        noise /= maxPossible;
        float height = 1.0f + noise * 0.095f;

        if (noise > 0.15f)
        {
            height += (noise - 0.15f) * 0.1125f;
        }

        if (height < 1.0f)
        {
            height = 1.0f + (height - 1.0f) * 0.3f;
        }

        return MathF.Max(0.9375f, MathF.Min(1.0625f, height));
    }

    private float NormalizedHeight(float height)
    {
        if (_maxHeight <= _minHeight)
        {
            return 0.5f;
        }

        return Math.Clamp((height - _minHeight) / (_maxHeight - _minHeight), 0f, 1f);
    }

    /// <summary>
    /// Samples the same terrain used on the globe at planetary longitude (0–360) and latitude (−90–90).
    /// </summary>
    public void SampleSurface(float longitudeDeg, float latitudeDeg, out Color color, out float normalizedHeight, out bool liquid)
    {
        Vector3 point = LonLatToUnitPoint(longitudeDeg, latitudeDeg);
        GetNoiseParameters(out float baseScale, out int octaves, out float persistence, out float lacunarity, out bool ridged);
        float maxPossible = NoiseAmplitudeSum(octaves, persistence);
        float height = ComputeRawHeight(point, baseScale, octaves, persistence, lacunarity, ridged, maxPossible);
        normalizedHeight = NormalizedHeight(height);
        color = GetSurfaceColor(point, normalizedHeight);
        liquid = IsLiquidTerrain(normalizedHeight);
    }

    public Color[] CreateAltitudeLegend()
    {
        Vector3 equator = new Vector3(1f, 0f, 0f);
        float[] samples = { 0.95f, 0.78f, 0.62f, 0.45f, 0.28f, 0.12f };
        var colors = new Color[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            colors[i] = GetSurfaceColor(equator, samples[i]);
        }

        return colors;
    }

    private static float NoiseAmplitudeSum(int octaves, float persistence)
    {
        float maxPossible = 0f;
        float amp = 1f;
        for (int i = 0; i < octaves; i++)
        {
            maxPossible += amp;
            amp *= persistence;
        }

        return maxPossible;
    }

    private static Vector3 LonLatToUnitPoint(float longitudeDeg, float latitudeDeg)
    {
        float lat = latitudeDeg * (MathF.PI / 180f);
        float lon = longitudeDeg * (MathF.PI / 180f);
        float ring = MathF.Cos(lat);
        return new Vector3(ring * MathF.Cos(lon), MathF.Sin(lat), ring * MathF.Sin(lon));
    }

    private bool IsLiquidTerrain(float normalizedHeight)
    {
        return TerrainStyle switch
        {
            PlanetTerrainStyle.Oceanic => normalizedHeight < 0.28f,
            PlanetTerrainStyle.Lush => normalizedHeight < 0.12f,
            PlanetTerrainStyle.Clouded => normalizedHeight < 0.18f,
            PlanetTerrainStyle.Ice => normalizedHeight < 0.20f,
            PlanetTerrainStyle.Volcanic => normalizedHeight < 0.34f,
            _ => false
        };
    }

    private Color GetVertexColor(int index)
    {
        return GetSurfaceColor(_spherePoints[index], NormalizedHeight(_pointHeights[index]));
    }

    private Color GetSurfaceColor(Vector3 point, float norm)
    {
        return TerrainStyle switch
        {
            PlanetTerrainStyle.Desert => ColorDesert(point, norm),
            PlanetTerrainStyle.Rocky => ColorRocky(point, norm),
            PlanetTerrainStyle.Ice => ColorIce(point, norm),
            PlanetTerrainStyle.Lush => ColorLush(point, norm),
            PlanetTerrainStyle.Clouded => ColorClouded(point, norm),
            PlanetTerrainStyle.GasBanded => ColorGasGiant(point, norm),
            PlanetTerrainStyle.IceBanded => ColorIceGiant(point, norm),
            PlanetTerrainStyle.Volcanic => ColorVolcanic(norm),
            _ => ColorOceanic(point, norm)
        };
    }

    private Color ColorOceanic(Vector3 point, float norm)
    {
        Color color;
        if (norm < 0.18f) color = new Color(10, 40, 140, 255);
        else if (norm < 0.28f) color = new Color(40, 100, 180, 255);
        else if (norm < 0.38f) color = new Color(180, 160, 100, 255);
        else if (norm < 0.55f) color = new Color(40, 140, 40, 255);
        else if (norm < 0.72f) color = new Color(120, 100, 80, 255);
        else if (norm < 0.88f) color = new Color(140, 140, 140, 255);
        else color = new Color(220, 230, 240, 255);

        return ApplyPolarIce(point, color, 0.78f);
    }

    private Color ColorLush(Vector3 point, float norm)
    {
        Color color;
        if (norm < 0.12f) color = new Color(18, 70, 140, 255);
        else if (norm < 0.22f) color = new Color(50, 120, 90, 255);
        else if (norm < 0.48f) color = new Color(36, 130, 48, 255);
        else if (norm < 0.68f) color = new Color(90, 120, 50, 255);
        else if (norm < 0.84f) color = new Color(110, 95, 70, 255);
        else color = new Color(200, 210, 215, 255);

        return ApplyPolarIce(point, color, 0.84f);
    }

    private Color ColorDesert(Vector3 point, float norm)
    {
        Color dark = Shade(SurfaceColor, 0.45f);
        Color mid = SurfaceColor;
        Color dune = Tint(SurfaceColor, new Color(220, 170, 90, 255), 0.35f);
        Color rock = new Color(70, 48, 36, 255);
        Color highland = new Color(160, 130, 100, 255);

        Color color;
        if (norm < 0.22f) color = dark;
        else if (norm < 0.4f) color = mid;
        else if (norm < 0.62f) color = dune;
        else if (norm < 0.82f) color = rock;
        else color = highland;

        float equator = 1f - MathF.Abs(point.Y);
        return LerpColor(color, Shade(color, 0.72f), equator * 0.18f);
    }

    private Color ColorRocky(Vector3 point, float norm)
    {
        Color baseRock = SurfaceColor;
        Color dark = Shade(baseRock, 0.4f);
        Color crater = Shade(baseRock, 0.22f);
        Color highland = Tint(baseRock, new Color(210, 205, 195, 255), 0.25f);

        if (norm > 0.88f)
        {
            return crater;
        }

        if (norm < 0.3f)
        {
            return dark;
        }

        Color color = LerpColor(baseRock, highland, norm);
        float polar = MathF.Abs(point.Y);
        if (polar > 0.88f)
        {
            return LerpColor(color, new Color(230, 230, 235, 255), (polar - 0.88f) / 0.12f);
        }

        return color;
    }

    private Color ColorIce(Vector3 point, float norm)
    {
        Color ice = Tint(SurfaceColor, new Color(230, 240, 250, 255), 0.35f);
        Color frost = new Color(200, 215, 230, 255);
        Color rock = new Color(90, 80, 70, 255);
        Color deep = Shade(ice, 0.55f);

        Color color;
        if (norm < 0.2f) color = deep;
        else if (norm < 0.45f) color = ice;
        else if (norm < 0.7f) color = frost;
        else color = rock;

        return ApplyPolarIce(point, color, 0.7f);
    }

    private Color ColorClouded(Vector3 point, float norm)
    {
        Color deep = Shade(SurfaceColor, 0.55f);
        Color mid = SurfaceColor;
        Color bright = Tint(SurfaceColor, new Color(255, 245, 220, 255), 0.5f);
        float swirl = 0.5f + 0.5f * MathF.Sin(point.Y * 9f + norm * 8f + point.X * 3f);
        Color color = LerpColor(LerpColor(deep, mid, swirl), bright, norm * 0.65f);
        return color;
    }

    private Color ColorGasGiant(Vector3 point, float norm)
    {
        Color dark = Shade(SurfaceColor, 0.42f);
        Color mid = SurfaceColor;
        Color light = Tint(SurfaceColor, new Color(255, 236, 200, 255), 0.4f);
        float band = MathF.Sin(point.Y * MathF.PI * _bandFrequency + norm * _bandTurbulence * 4f);
        float t = 0.5f + 0.5f * band;
        Color color = LerpColor(dark, LerpColor(mid, light, t), 0.35f + 0.65f * t);

        if (norm > 0.86f && MathF.Abs(point.Y) < 0.38f)
        {
            Color storm = Tint(SurfaceColor, new Color(180, 50, 40, 255), 0.55f);
            color = LerpColor(color, storm, 0.7f);
        }

        return color;
    }

    private Color ColorIceGiant(Vector3 point, float norm)
    {
        Color deep = Shade(SurfaceColor, 0.5f);
        Color mid = SurfaceColor;
        Color haze = Tint(SurfaceColor, new Color(210, 235, 250, 255), 0.45f);
        float band = MathF.Sin(point.Y * MathF.PI * _bandFrequency + norm * _bandTurbulence * 3f);
        float t = 0.5f + 0.5f * band;
        Color color = LerpColor(deep, LerpColor(mid, haze, t), 0.4f + 0.6f * t);
        return ApplyPolarIce(point, color, 0.72f);
    }

    private Color ColorVolcanic(float norm)
    {
        if (norm < 0.22f) return new Color(255, 220, 80, 255);
        if (norm < 0.34f) return new Color(230, 90, 20, 255);
        if (norm < 0.46f) return new Color(140, 40, 20, 255);
        if (norm < 0.7f) return Shade(SurfaceColor, 0.55f);
        return Shade(SurfaceColor, 0.28f);
    }

    private Color ApplyPolarIce(Vector3 point, Color color, float startAbsY)
    {
        float polar = MathF.Abs(point.Y);
        if (polar <= startAbsY)
        {
            return color;
        }

        float t = Math.Clamp((polar - startAbsY) / (1f - startAbsY), 0f, 1f);
        return LerpColor(color, new Color(230, 238, 245, 255), t);
    }

    private static Color Shade(Color color, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        return new Color(
            (byte)(color.R * amount),
            (byte)(color.G * amount),
            (byte)(color.B * amount),
            (byte)255);
    }

    private static Color Tint(Color color, Color toward, float t)
    {
        return LerpColor(color, toward, t);
    }

    private static Color LerpColor(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new Color(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t),
            (byte)255);
    }

    /// <summary>
    /// Outer ring radius in the same display units as <paramref name="displayRadius"/>; 0 if no rings.
    /// Must match scaling in <see cref="DrawPlanetRingAnnulus"/>.
    /// </summary>
    private float GetRingOuterRadiusDisplay(float displayRadius)
    {
        if (!Rings.HasValue || !Rings.Value.IsValid)
        {
            return 0f;
        }

        PlanetRingData ring = Rings.Value;

        if (RadiusKm > 0f)
        {
            float kmToDisplay = displayRadius / RadiusKm;
            return ring.OuterRadiusKm * kmToDisplay;
        }

        float kmToDisplayFallback = displayRadius / MathF.Max(ring.OuterRadiusKm, 1f);
        return ring.OuterRadiusKm * kmToDisplayFallback;
    }

    public void DrawSpherePointsToTexture(RenderTexture2D target, float displayRadius, float rotationAngle = 0f)
    {
        // When rings extend past the sphere, zoom out by uniformly shrinking the scene so the
        // outer ring fits. Do not move the camera farther: Raylib's default perspective far plane
        // (~1000) would clip the whole scene if the camera is pushed too far back.
        float ringOuter = GetRingOuterRadiusDisplay(displayRadius);
        float maxExtent = MathF.Max(displayRadius, ringOuter);
        if (maxExtent < 1e-6f)
        {
            maxExtent = displayRadius;
        }

        float worldScale = displayRadius / maxExtent;

        float cameraDistance = displayRadius * 2.5f;
        float sphereRadius = displayRadius * worldScale;
        Camera3D camera = new Camera3D
        {
            Position = new Vector3(0, 0, cameraDistance), // Camera positioned along +Z axis
            Target = Vector3.Zero, // Looking at origin
            Up = new Vector3(0, 1, 0), // Y is up
            FovY = 60.0f, // Field of view
            Projection = CameraProjection.CAMERA_PERSPECTIVE
        };

        // Calculate rotation matrix around Y axis
        float c = MathF.Cos(rotationAngle);
        float s = MathF.Sin(rotationAngle);
        
        // Transform function for rotating a point around Y axis
        Vector3 RotateY(Vector3 v)
        {
            return new Vector3(
                v.X * c - v.Z * s,
                v.Y,
                v.X * s + v.Z * c
            );
        }

        // Begin rendering to texture
        Raylib.BeginTextureMode(target);
        Raylib.ClearBackground(Color.BLACK);

        // Begin 3D mode
        Raylib.BeginMode3D(camera);

        if (string.Equals(Name, "Jupiter", StringComparison.OrdinalIgnoreCase)
            && PlanetSphereTextureResources.TryGetJupiter(out Texture2D jupiterTex))
        {
            PlanetSphereTextureResources.DrawJupiterTextured(jupiterTex, sphereRadius, rotationAngle);
        }
        else
        {
            Rlgl.EnableDepthTest();
            Rlgl.EnableBackfaceCulling();

            // Draw all triangles with rotation and scale applied (procedural noise colors)
            foreach (var tri in _triangles)
            {
                Vector3 v1 = RotateY(tri.V1) * sphereRadius;
                Vector3 v2 = RotateY(tri.V2) * sphereRadius;
                Vector3 v3 = RotateY(tri.V3) * sphereRadius;

                Raylib.DrawTriangle3D(v1, v2, v3, tri.Color);
            }
        }

        DrawPlanetRingAnnulus(displayRadius, RotateY, worldScale);

        // End 3D mode
        Raylib.EndMode3D();

        // End rendering to texture
        Raylib.EndTextureMode();
    }

    /// <summary>
    /// Single flat annulus in the equatorial plane, tilted so the camera sees an ellipse; drawn after the sphere for depth.
    /// Uses <see cref="Rings"/> when present and valid; skips drawing when the planet has no ring system.
    /// Spin is applied in the equatorial plane (same Y-axis rotation as the sphere), then tilt.
    /// </summary>
    private void DrawPlanetRingAnnulus(float displayRadius, Func<Vector3, Vector3> rotateY, float worldScale = 1f)
    {
        if (!Rings.HasValue || !Rings.Value.IsValid)
        {
            return;
        }

        PlanetRingData ring = Rings.Value;
        float innerR;
        float outerR;
        if (RadiusKm > 0f)
        {
            float kmToDisplay = displayRadius / RadiusKm;
            innerR = ring.InnerRadiusKm * kmToDisplay;
            outerR = ring.OuterRadiusKm * kmToDisplay;
        }
        else
        {
            // No equatorial radius in catalog: scale so outer ring radius matches display sphere radius for a sane fallback.
            float kmToDisplay = displayRadius / MathF.Max(ring.OuterRadiusKm, 1f);
            innerR = ring.InnerRadiusKm * kmToDisplay;
            outerR = ring.OuterRadiusKm * kmToDisplay;
        }

        innerR *= worldScale;
        outerR *= worldScale;

        if (innerR <= 0f || outerR <= innerR)
        {
            return;
        }

        if (RingTextureResources.TryGetSaturnRingAlpha(out Texture2D ringTex))
        {
            DrawPlanetRingAnnulusTextured(innerR, outerR, ring, rotateY, ringTex);
            return;
        }

        const int segments = 96;
        const float tiltDeg = 26f;
        float ringOpacity = Math.Clamp(ring.Opacity, 0f, 1f);
        Color ringRgb = ring.RingColor;

        float tiltRad = tiltDeg * (MathF.PI / 180f);
        float cosT = MathF.Cos(tiltRad);
        float sinT = MathF.Sin(tiltRad);

        Vector3 TiltEquatorial(Vector3 v)
        {
            return new Vector3(
                v.X,
                v.Y * cosT - v.Z * sinT,
                v.Y * sinT + v.Z * cosT);
        }

        // Spin in the equatorial (xz) plane first — same Y-axis rotation as the sphere — then tilt
        // for viewing. Order Tilt(RotateY(flat)) not RotateY(Tilt(flat)) so the ring co-rotates with
        // the planet; RotateY(Tilt(...)) tilts in world space first and the ring no longer shares the spin axis.
        Vector3 TransformRingPoint(float radius, float angleRad)
        {
            float cx = radius * MathF.Cos(angleRad);
            float cz = radius * MathF.Sin(angleRad);
            Vector3 flat = new Vector3(cx, 0f, cz);
            Vector3 spun = rotateY(flat);
            return TiltEquatorial(spun);
        }

        Raylib.BeginBlendMode(BlendMode.BLEND_ALPHA);

        byte baseA = (byte)Math.Clamp((int)(ringOpacity * 255f), 8, 255);
        Color c = ringRgb;
        Color cTop = new Color(c.R, c.G, c.B, baseA);
        Color cBot = new Color(
            (byte)Math.Min(255, c.R + 18),
            (byte)Math.Min(255, c.G + 18),
            (byte)Math.Min(255, c.B + 18),
            (byte)Math.Clamp(baseA * 3 / 4, 6, 255));

        void DrawAnnulusBand()
        {
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * MathF.Tau / segments;
                float a1 = (i + 1) * MathF.Tau / segments;

                Vector3 i0 = TransformRingPoint(innerR, a0);
                Vector3 i1 = TransformRingPoint(innerR, a1);
                Vector3 o0 = TransformRingPoint(outerR, a0);
                Vector3 o1 = TransformRingPoint(outerR, a1);

                // Quad i0 → i1 → o1 → o0 (two triangles). CCW from one side of the ring plane = front;
                // reverse vertex order = back, so both sides render with backface culling on.
                Raylib.DrawTriangle3D(i0, i1, o1, cTop);
                Raylib.DrawTriangle3D(i0, o1, o0, cTop);
                Raylib.DrawTriangle3D(i0, o1, i1, cBot);
                Raylib.DrawTriangle3D(i0, o0, o1, cBot);
            }
        }

        DrawAnnulusBand();

        Raylib.EndBlendMode();
    }

    /// <summary>
    /// Saturn ring strip texture (CC BY 4.0, Solar System Scope): UVs use U = radial (inner→outer), V = angle,
    /// i.e. a 90° rotation from the image’s wide-horizontal layout so it matches the annulus mesh.
    /// </summary>
    private void DrawPlanetRingAnnulusTextured(
        float innerR,
        float outerR,
        PlanetRingData ring,
        Func<Vector3, Vector3> rotateY,
        Texture2D ringTexture)
    {
        const int segments = 96;
        const float tiltDeg = 26f;
        float ringOpacity = Math.Clamp(ring.Opacity, 0f, 1f);
        byte baseA = (byte)Math.Clamp((int)(ringOpacity * 255f), 8, 255);

        float tiltRad = tiltDeg * (MathF.PI / 180f);
        float cosT = MathF.Cos(tiltRad);
        float sinT = MathF.Sin(tiltRad);

        Vector3 TiltEquatorial(Vector3 v)
        {
            return new Vector3(
                v.X,
                v.Y * cosT - v.Z * sinT,
                v.Y * sinT + v.Z * cosT);
        }

        Vector3 TransformRingPoint(float radius, float angleRad)
        {
            float cx = radius * MathF.Cos(angleRad);
            float cz = radius * MathF.Sin(angleRad);
            Vector3 flat = new Vector3(cx, 0f, cz);
            Vector3 spun = rotateY(flat);
            return TiltEquatorial(spun);
        }

        Raylib.BeginBlendMode(BlendMode.BLEND_ALPHA);

        Rlgl.SetTexture(ringTexture.Id);
        Rlgl.Begin((int)DrawMode.QUADS);

        for (int i = 0; i < segments; i++)
        {
            float a0 = i * MathF.Tau / segments;
            float a1 = (i + 1) * MathF.Tau / segments;
            float u0 = i / (float)segments;
            float u1 = (i + 1) / (float)segments;

            Vector3 i0 = TransformRingPoint(innerR, a0);
            Vector3 i1 = TransformRingPoint(innerR, a1);
            Vector3 o0 = TransformRingPoint(outerR, a0);
            Vector3 o1 = TransformRingPoint(outerR, a1);

            // Front (+normal): CCW when viewed from above the ring plane.
            // Texture UVs rotated 90°: U = radial (0 inner, 1 outer), V = angle around the ring.
            Rlgl.Color4ub(255, 255, 255, baseA);
            Rlgl.TexCoord2f(0f, u0);
            Rlgl.Vertex3f(i0.X, i0.Y, i0.Z);
            Rlgl.TexCoord2f(0f, u1);
            Rlgl.Vertex3f(i1.X, i1.Y, i1.Z);
            Rlgl.TexCoord2f(1f, u1);
            Rlgl.Vertex3f(o1.X, o1.Y, o1.Z);
            Rlgl.TexCoord2f(1f, u0);
            Rlgl.Vertex3f(o0.X, o0.Y, o0.Z);

            // Back: reverse winding so both sides draw with culling on.
            Rlgl.Color4ub(255, 255, 255, baseA);
            Rlgl.TexCoord2f(0f, u0);
            Rlgl.Vertex3f(i0.X, i0.Y, i0.Z);
            Rlgl.TexCoord2f(1f, u0);
            Rlgl.Vertex3f(o0.X, o0.Y, o0.Z);
            Rlgl.TexCoord2f(1f, u1);
            Rlgl.Vertex3f(o1.X, o1.Y, o1.Z);
            Rlgl.TexCoord2f(0f, u1);
            Rlgl.Vertex3f(i1.X, i1.Y, i1.Z);
        }

        Rlgl.End();
        Rlgl.SetTexture(0);

        Raylib.EndBlendMode();
    }
}

// Self-contained 3D noise for terrain color sampling (heights are not applied to vertex radius).
internal static class Noise3D
{
    private static readonly int[] Perm = new int[512];

    static Noise3D()
    {
        int[] p =
        {
            151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225,
            140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148,
            247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32,
            57, 177, 33, 88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175,
            74, 165, 71, 134, 139, 48, 27, 166, 77, 146, 158, 231, 83, 111, 229, 122,
            60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244, 102, 143, 54,
            65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169,
            200, 196, 135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64,
            52, 217, 226, 250, 124, 123, 5, 202, 38, 147, 118, 126, 255, 82, 85, 212,
            207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42, 223, 183, 170, 213,
            119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9,
            129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104,
            218, 246, 97, 228, 251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162, 241,
            81, 51, 145, 235, 249, 14, 239, 107, 49, 192, 214, 31, 181, 199, 106, 157,
            184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254, 138, 236, 205, 93,
            222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180
        };

        for (int i = 0; i < 512; i++)
        {
            Perm[i] = p[i % 256];
        }
    }

    public static float Get(float x, float y, float z)
    {
        int X = (int)Math.Floor(x) & 255;
        int Y = (int)Math.Floor(y) & 255;
        int Z = (int)Math.Floor(z) & 255;

        x -= (float)Math.Floor(x);
        y -= (float)Math.Floor(y);
        z -= (float)Math.Floor(z);

        float u = Fade(x);
        float v = Fade(y);
        float w = Fade(z);

        int A  = Perm[X]   + Y;
        int AA = Perm[A]   + Z;
        int AB = Perm[A+1] + Z;
        int B  = Perm[X+1] + Y;
        int BA = Perm[B]   + Z;
        int BB = Perm[B+1] + Z;

        return Lerp(w,
            Lerp(v,
                Lerp(u, Grad(Perm[AA],   x,   y,   z  ),
                       Grad(Perm[BA],   x-1, y,   z  )),
                Lerp(u, Grad(Perm[AB],   x,   y-1, z  ),
                       Grad(Perm[BB],   x-1, y-1, z  ))),
            Lerp(v,
                Lerp(u, Grad(Perm[AA+1], x,   y,   z-1),
                       Grad(Perm[BA+1], x-1, y,   z-1)),
                Lerp(u, Grad(Perm[AB+1], x,   y-1, z-1),
                       Grad(Perm[BB+1], x-1, y-1, z-1))));
    }

    private static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);
    private static float Lerp(float t, float a, float b) => a + t * (b - a);

    private static float Grad(int hash, float x, float y, float z)
    {
        int h = hash & 15;
        float u = h < 8 ? x : y;
        float v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }
}

