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

    private List<Vector3> _spherePoints = new();
    private List<float> _pointHeights = new();
    private List<List<int>> _ringIndices = new();
    private Random _random;
    private Vector3 _noiseOffset; // Offset for noise sampling to make each planet unique

    private float _minHeight = float.MaxValue;
    private float _maxHeight = float.MinValue;
    private List<TriangleData> _triangles = new();

    public Planet(string name, Vector2 position, float radius, Color surfaceColor)
    {
        Name = name;
        Position = position;
        Radius = radius;
        SurfaceColor = surfaceColor;
        int seed = name.GetHashCode();
        _random = new Random(seed);
        
        // Create a unique noise offset using the seeded random number generator
        // This ensures each planet samples different parts of the noise space
        // Using Random ensures variation even if hash codes are similar
        _noiseOffset = new Vector3(
            _random.Next(100000, 999999),
            _random.Next(100000, 999999),
            _random.Next(100000, 999999)
        );

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

        // Assign heights using 3D noise → no grid, no helix, no seams
        AssignHeightsWith3DNoise();
    }

    private void BuildTriangles()
    {
        // Build triangles from rings with heights applied using fractional mapping
        for (int ring = 0; ring < _ringIndices.Count - 1; ring++)
        {
            List<int> currentRing = _ringIndices[ring];
            List<int> nextRing = _ringIndices[ring + 1];

            // Handle poles (single point rings)
            if (currentRing.Count == 1)
            {
                // Connect pole to first ring - full fan coverage
                int poleIdx = currentRing[0];
                Vector3 poleVertex = _spherePoints[poleIdx] * _pointHeights[poleIdx];
                Color poleColor = GetColorForHeight(_pointHeights[poleIdx]);
                
                for (int i = 0; i < nextRing.Count; i++)
                {
                    int nextIdx = nextRing[i];
                    int nextIdx2 = nextRing[(i + 1) % nextRing.Count];

                    Vector3 v1 = poleVertex;
                    Vector3 v2 = _spherePoints[nextIdx] * _pointHeights[nextIdx];
                    Vector3 v3 = _spherePoints[nextIdx2] * _pointHeights[nextIdx2];
                    
                    // Ensure outward-facing winding (counter-clockwise from outside)
                    TriangleData tri = EnsureOutwardFacing(new TriangleData { V1 = v1, V2 = v2, V3 = v3, Color = AverageColor(poleColor, GetColorForHeight(_pointHeights[nextIdx]), GetColorForHeight(_pointHeights[nextIdx2])) });
                    _triangles.Add(tri);
                }
            }
            else if (nextRing.Count == 1)
            {
                // Connect last ring to pole - full fan coverage
                int poleIdx = nextRing[0];
                Vector3 poleVertex = _spherePoints[poleIdx] * _pointHeights[poleIdx];
                Color poleColor = GetColorForHeight(_pointHeights[poleIdx]);
                
                for (int i = 0; i < currentRing.Count; i++)
                {
                    int currIdx = currentRing[i];
                    int currIdx2 = currentRing[(i + 1) % currentRing.Count];

                    Vector3 v1 = _spherePoints[currIdx] * _pointHeights[currIdx];
                    Vector3 v2 = _spherePoints[currIdx2] * _pointHeights[currIdx2];
                    Vector3 v3 = poleVertex;
                    
                    // Ensure outward-facing winding
                    TriangleData tri = EnsureOutwardFacing(new TriangleData { V1 = v1, V2 = v2, V3 = v3, Color = AverageColor(GetColorForHeight(_pointHeights[currIdx]), GetColorForHeight(_pointHeights[currIdx2]), poleColor) });
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
                        Vector3 v1 = _spherePoints[idx1] * _pointHeights[idx1];
                        Vector3 v2 = _spherePoints[idx2] * _pointHeights[idx2];
                        Vector3 v3 = _spherePoints[idx3] * _pointHeights[idx3];
                        Color avgColor = AverageColor(GetColorForHeight(_pointHeights[idx1]), GetColorForHeight(_pointHeights[idx2]), GetColorForHeight(_pointHeights[idx3]));
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
        const float baseScale = 2.2f;     // larger = bigger continents
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
            Vector3 p = _spherePoints[i];  // unit vector

            // Fractional Brownian Motion (fBm) in 3D
            float noise = 0f;
            float frequency = baseScale;
            amp = 1f;

            for (int o = 0; o < octaves; o++)
            {
                // Add noise offset to ensure each planet samples different parts of noise space
                float sample = Noise3D.Get(
                    (p.X * frequency) + _noiseOffset.X,
                    (p.Y * frequency) + _noiseOffset.Y,
                    (p.Z * frequency) + _noiseOffset.Z
                );
                noise += sample * amp;
                amp *= persistence;
                frequency *= lacunarity;
            }

            noise /= maxPossible;           // ~ -1 .. 1
            float height = 1.0f + noise * 0.095f; // Reduced by half again: was 0.19f

            // Optional ridge / mountain sharpening
            if (noise > 0.15f)
                height += (noise - 0.15f) * 0.1125f; // Reduced by half again: was 0.225f

            // Optional ocean flattening
            if (height < 1.0f)
                height = 1.0f + (height - 1.0f) * 0.3f;

            height = MathF.Max(0.9375f, MathF.Min(1.0625f, height)); // Reduced range by half again: was [0.875, 1.125], now [0.9375, 1.0625]

            _pointHeights[i] = height;

            _minHeight = Math.Min(_minHeight, height);
            _maxHeight = Math.Max(_maxHeight, height);
        }
    }

    private Color GetColorForHeight(float height)
    {
        if (_maxHeight <= _minHeight) return Color.GREEN;

        float norm = (height - _minHeight) / (_maxHeight - _minHeight);

        if (norm < 0.18f) return new Color(10, 40, 140, 255);     // deep ocean
        if (norm < 0.28f) return new Color(40, 100, 180, 255);    // shallow
        if (norm < 0.38f) return new Color(180, 160, 100, 255);   // beach
        if (norm < 0.55f) return new Color(40, 140, 40, 255);     // grass/forest
        if (norm < 0.72f) return new Color(120, 100, 80, 255);    // hills/rock
        if (norm < 0.88f) return new Color(140, 140, 140, 255);   // mountains
        return new Color(220, 230, 240, 255);                      // snow
    }

    public void DrawSpherePoints(Vector2 center, float displayRadius, float rotationAngle = 0f)
    {
        // Set up 3D camera looking at planet head-on (along Z-axis)
        // Position camera far enough away to see the entire planet (2.5x the radius)
        float cameraDistance = displayRadius * 2.5f;
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

        // Begin 3D mode
        Raylib.BeginMode3D(camera);

        // Draw all triangles with rotation and scale applied
        foreach (var tri in _triangles)
        {
            // Apply rotation and scale to vertices
            Vector3 v1 = RotateY(tri.V1) * displayRadius;
            Vector3 v2 = RotateY(tri.V2) * displayRadius;
            Vector3 v3 = RotateY(tri.V3) * displayRadius;
            
            // Draw triangle in 3D (backface culling handled automatically)
            Raylib.DrawTriangle3D(v1, v2, v3, tri.Color);
        }

        // End 3D mode
        Raylib.EndMode3D();
    }


}

// Self-contained 3D noise helper (no external library needed)
internal static class Noise3D
{
    private static readonly int[] Perm = new int[512];

    static Noise3D()
    {
        int[] p = new int[256];
        for (int i = 0; i < 256; i++) p[i] = i;

        // Simple deterministic shuffle (you can replace with seeded Random if desired)
        for (int i = 255; i > 0; i--)
        {
            int j = i; // or use a hash here
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