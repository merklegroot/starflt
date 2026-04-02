using Raylib_cs;
using System.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StarflightGame;

public class Planet
{
    public string Name { get; set; } = "";
    public Vector2 Position { get; set; }
    public float Radius { get; set; }
    public Color SurfaceColor { get; set; }

    /// <summary>Equatorial radius in kilometers when known from catalog data; 0 if not specified.</summary>
    public float RadiusKm { get; set; }

    /// <summary>Ring system from catalog data, if any.</summary>
    public PlanetRingData? Rings { get; set; }

    /// <summary>Broad composition from catalog data when the planet was loaded from <c>planets.json</c>.</summary>
    public PlanetComposition Composition { get; set; }

    private List<Vector3> _spherePoints = new();
    private List<float> _pointHeights = new();
    private List<List<int>> _ringIndices = new();
    private List<TriangleData> _triangles = new();
    private Random _random;
    private Vector3 _noiseOffset;
    private float _minHeight = float.MaxValue;
    private float _maxHeight = float.MinValue;

    public Planet(
        string name,
        Vector2 position,
        float radius,
        Color surfaceColor,
        float radiusKm = 0f,
        PlanetRingData? rings = null,
        PlanetComposition composition = PlanetComposition.Terrestrial)
    {
        Name = name;
        Position = position;
        Radius = radius;
        SurfaceColor = surfaceColor;
        RadiusKm = radiusKm;
        Rings = rings;
        Composition = composition;

        int seed = name.GetHashCode();
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
                Color poleColor = GetColorForHeight(_pointHeights[poleIdx]);

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
                        Color = AverageColor(poleColor, GetColorForHeight(_pointHeights[nextIdx]), GetColorForHeight(_pointHeights[nextIdx2]))
                    });
                    _triangles.Add(tri);
                }
            }
            else if (nextRing.Count == 1)
            {
                // Connect last ring to pole - full fan coverage
                int poleIdx = nextRing[0];
                Vector3 poleVertex = _spherePoints[poleIdx];
                Color poleColor = GetColorForHeight(_pointHeights[poleIdx]);

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
                        Color = AverageColor(GetColorForHeight(_pointHeights[currIdx]), GetColorForHeight(_pointHeights[currIdx2]), poleColor)
                    });
                    _triangles.Add(tri);
                }
            }
            else
            {
                // Connect two rings with density-aware fan/quad fill
                // This ensures every point is connected, even when point counts differ dramatically
                int n1 = currentRing.Count;
                int n2 = nextRing.Count;
                float ratio = n2 / (float)n1;
                
                // Build a set to track which connections we've made (to avoid duplicates)
                HashSet<(int, int, int)> addedTriangles = new HashSet<(int, int, int)>();
                
                // Helper to add triangle if not already added
                void AddTriangleIfNew(int idx1, int idx2, int idx3)
                {
                    // Normalize triangle indices (smallest first) for duplicate detection
                    int min = Math.Min(Math.Min(idx1, idx2), idx3);
                    int max = Math.Max(Math.Max(idx1, idx2), idx3);
                    int mid = idx1 + idx2 + idx3 - min - max;
                    var key = (min, mid, max);
                    
                    if (!addedTriangles.Contains(key))
                    {
                        addedTriangles.Add(key);
                        Vector3 v1 = _spherePoints[idx1];
                        Vector3 v2 = _spherePoints[idx2];
                        Vector3 v3 = _spherePoints[idx3];
                        Color avgColor = AverageColor(
                            GetColorForHeight(_pointHeights[idx1]),
                            GetColorForHeight(_pointHeights[idx2]),
                            GetColorForHeight(_pointHeights[idx3]));
                        TriangleData tri = EnsureOutwardFacing(new TriangleData { V1 = v1, V2 = v2, V3 = v3, Color = avgColor });
                        _triangles.Add(tri);
                    }
                }
                
                // For each edge in current ring, connect to all points in next ring that map to it
                for (int i = 0; i < n1; i++)
                {
                    int currIdx = currentRing[i];
                    int currIdx2 = currentRing[(i + 1) % n1]; // Wrap for seam
                    
                    // Use fractional mapping to find range of points in next ring
                    float mappedStart = i * ratio;
                    float mappedEnd = (i + 1) * ratio;
                    if (i + 1 == n1) mappedEnd = n2; // Explicitly handle seam
                    
                    int nextStart = WrapIndex((int)MathF.Floor(mappedStart), n2);
                    int nextEnd = WrapIndex((int)MathF.Ceiling(mappedEnd - 0.0001f), n2); // Use slightly less than mappedEnd to avoid including next edge's start
                    
                    // Collect all points in next ring that should connect to this edge
                    List<int> nextPoints = new List<int>();
                    
                    // Add points from nextStart to nextEnd (handling wrap-around)
                    if (nextEnd >= nextStart)
                    {
                        // Normal case: no wrap-around
                        for (int j = nextStart; j <= nextEnd; j++)
                        {
                            nextPoints.Add(nextRing[j]);
                        }
                    }
                    else
                    {
                        // Wrap-around case: nextEnd < nextStart means we wrapped past the seam
                        for (int j = nextStart; j < n2; j++)
                        {
                            nextPoints.Add(nextRing[j]);
                        }
                        for (int j = 0; j <= nextEnd; j++)
                        {
                            nextPoints.Add(nextRing[j]);
                        }
                    }
                    
                    // Ensure we have at least one point (should always be true)
                    if (nextPoints.Count == 0)
                    {
                        nextPoints.Add(nextRing[nextStart]);
                    }
                    
                    // Remove duplicates while preserving order
                    nextPoints = nextPoints.Distinct().ToList();
                    
                    // Connect edge currIdx->currIdx2 to all points in nextPoints using fan pattern
                    // This ensures complete coverage
                    if (nextPoints.Count == 1)
                    {
                        // Single point: just one triangle
                        AddTriangleIfNew(currIdx, currIdx2, nextPoints[0]);
                    }
                    else
                    {
                        // Multiple points: fan pattern
                        // First triangle: currIdx, currIdx2, first point
                        AddTriangleIfNew(currIdx, currIdx2, nextPoints[0]);
                        
                        // Fan: connect each consecutive pair to currIdx2
                        for (int k = 0; k < nextPoints.Count - 1; k++)
                        {
                            AddTriangleIfNew(currIdx2, nextPoints[k], nextPoints[k + 1]);
                        }
                        
                        // Also connect currIdx to points for better quad coverage
                        for (int k = 1; k < nextPoints.Count; k++)
                        {
                            AddTriangleIfNew(currIdx, nextPoints[k - 1], nextPoints[k]);
                        }
                    }
                }
            }
        }
    }

    private int WrapIndex(int index, int count)
    {
        // Wrap index to valid range [0, count)
        index = index % count;
        if (index < 0) index += count;
        return index;
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

    private void AssignHeightsWith3DNoise()
    {
        const float baseScale = 2.2f;
        const int octaves = 7;
        const float persistence = 0.48f;
        const float lacunarity = 2.1f;

        float maxPossible = 0f;
        float amp = 1f;
        for (int i = 0; i < octaves; i++)
        {
            maxPossible += amp;
            amp *= persistence;
        }

        for (int i = 0; i < _spherePoints.Count; i++)
        {
            Vector3 p = _spherePoints[i];

            float noise = 0f;
            float frequency = baseScale;
            amp = 1f;

            for (int o = 0; o < octaves; o++)
            {
                float sample = Noise3D.Get(
                    (p.X * frequency) + _noiseOffset.X,
                    (p.Y * frequency) + _noiseOffset.Y,
                    (p.Z * frequency) + _noiseOffset.Z);
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

            height = MathF.Max(0.9375f, MathF.Min(1.0625f, height));

            _pointHeights[i] = height;

            _minHeight = Math.Min(_minHeight, height);
            _maxHeight = Math.Max(_maxHeight, height);
        }
    }

    private Color GetColorForHeight(float height)
    {
        if (_maxHeight <= _minHeight)
        {
            return Color.GREEN;
        }

        float norm = (height - _minHeight) / (_maxHeight - _minHeight);

        if (norm < 0.18f) return new Color(10, 40, 140, 255);
        if (norm < 0.28f) return new Color(40, 100, 180, 255);
        if (norm < 0.38f) return new Color(180, 160, 100, 255);
        if (norm < 0.55f) return new Color(40, 140, 40, 255);
        if (norm < 0.72f) return new Color(120, 100, 80, 255);
        if (norm < 0.88f) return new Color(140, 140, 140, 255);
        return new Color(220, 230, 240, 255);
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
        int[] p = new int[256];
        for (int i = 0; i < 256; i++) p[i] = i;

        for (int i = 255; i > 0; i--)
        {
            int j = i;
            (p[i], p[j]) = (p[j], p[i]);
        }

        for (int i = 0; i < 512; i++) Perm[i] = p[i % 256];
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

