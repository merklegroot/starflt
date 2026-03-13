using Raylib_cs;
using System.Numerics;
using System;
using System.Collections.Generic;

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

    private float _minHeight = float.MaxValue;
    private float _maxHeight = float.MinValue;

    public Planet(string name, Vector2 position, float radius, Color surfaceColor)
    {
        Name = name;
        Position = position;
        Radius = radius;
        SurfaceColor = surfaceColor;
        _random = new Random(name.GetHashCode());

        GenerateSpherePoints();
    }

    private void GenerateSpherePoints()
    {
        const int pointsPerRing = 80;
        const int numRings = 60;

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
                float sample = Noise3D.Get(p.X * frequency, p.Y * frequency, p.Z * frequency);
                noise += sample * amp;
                amp *= persistence;
                frequency *= lacunarity;
            }

            noise /= maxPossible;           // ~ -1 .. 1
            float height = 1.0f + noise * 0.38f;

            // Optional ridge / mountain sharpening
            if (noise > 0.15f)
                height += (noise - 0.15f) * 0.45f;

            // Optional ocean flattening
            if (height < 1.0f)
                height = 1.0f + (height - 1.0f) * 0.3f;

            height = MathF.Max(0.75f, MathF.Min(1.45f, height));

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

    private Vector2 Project3DTo2D(Vector3 p3d, Vector2 center, float displayRadius)
    {
        return new Vector2(
            center.X + p3d.X * displayRadius,
            center.Y + p3d.Y * displayRadius
        );
    }

    private Vector3 TransformPoint(Vector3 unitPoint, float height, float rotationY)
    {
        Vector3 scaled = unitPoint * height;

        float c = MathF.Cos(rotationY);
        float s = MathF.Sin(rotationY);

        return new Vector3(
            scaled.X * c - scaled.Z * s,
            scaled.Y,
            scaled.X * s + scaled.Z * c
        );
    }

    private void DrawTriangleWithBackface(Vector2 v1, Vector2 v2, Vector2 v3, Vector3 p1_3d, Vector3 p2_3d, Vector3 p3_3d, Color color)
    {
        // Calculate triangle normal in 3D space
        Vector3 edge1 = p2_3d - p1_3d;
        Vector3 edge2 = p3_3d - p1_3d;
        Vector3 normal = Vector3.Cross(edge1, edge2);
        
        // Camera is looking along -Z axis (from positive Z towards origin in 2D projection)
        // For 2D orthographic projection, we want to draw triangles facing the camera
        // The normal's Z component tells us which way the triangle is facing
        // Since we're doing a simple 2D projection (ignoring Z), draw both orientations to be safe
        // This ensures all triangles are visible regardless of rotation
        
        // Draw in original order
        Raylib.DrawTriangle(v1, v2, v3, color);
        // Also draw in reverse order to ensure back-facing triangles are visible
        Raylib.DrawTriangle(v3, v2, v1, color);
    }

    public void DrawSpherePoints(Vector2 center, float displayRadius, float rotationAngle = 0f)
    {
        // Transform all points to screen space and 3D space
        Vector2[] screenPoints = new Vector2[_spherePoints.Count];
        Vector3[] transformed3DPoints = new Vector3[_spherePoints.Count];
        Color[] pointColors = new Color[_spherePoints.Count];

        for (int i = 0; i < _spherePoints.Count; i++)
        {
            Vector3 p = TransformPoint(_spherePoints[i], _pointHeights[i], rotationAngle);
            transformed3DPoints[i] = p;
            screenPoints[i] = Project3DTo2D(p, center, displayRadius);
            pointColors[i] = GetColorForHeight(_pointHeights[i]);
        }

        // Draw triangles between rings
        for (int ring = 0; ring < _ringIndices.Count - 1; ring++)
        {
            List<int> currentRing = _ringIndices[ring];
            List<int> nextRing = _ringIndices[ring + 1];

            // Handle poles (single point rings)
            if (currentRing.Count == 1)
            {
                // Connect pole to first ring
                int poleIdx = currentRing[0];
                for (int i = 0; i < nextRing.Count; i++)
                {
                    int nextIdx = nextRing[i];
                    int nextIdx2 = nextRing[(i + 1) % nextRing.Count];

                    Color avgColor = AverageColor(pointColors[poleIdx], pointColors[nextIdx], pointColors[nextIdx2]);
                    DrawTriangleWithBackface(
                        screenPoints[poleIdx], screenPoints[nextIdx], screenPoints[nextIdx2],
                        transformed3DPoints[poleIdx], transformed3DPoints[nextIdx], transformed3DPoints[nextIdx2],
                        avgColor);
                }
            }
            else if (nextRing.Count == 1)
            {
                // Connect last ring to pole
                int poleIdx = nextRing[0];
                for (int i = 0; i < currentRing.Count; i++)
                {
                    int currIdx = currentRing[i];
                    int currIdx2 = currentRing[(i + 1) % currentRing.Count];

                    Color avgColor = AverageColor(pointColors[currIdx], pointColors[currIdx2], pointColors[poleIdx]);
                    DrawTriangleWithBackface(
                        screenPoints[currIdx], screenPoints[currIdx2], screenPoints[poleIdx],
                        transformed3DPoints[currIdx], transformed3DPoints[currIdx2], transformed3DPoints[poleIdx],
                        avgColor);
                }
            }
            else
            {
                // Connect two rings with multiple points
                // Simple and reliable: connect each edge in current ring to nearest edges in next ring
                for (int i = 0; i < currentRing.Count; i++)
                {
                    int currIdx = currentRing[i];
                    int currIdx2 = currentRing[(i + 1) % currentRing.Count];
                    
                    // Calculate phi angle for this edge's midpoint
                    float currPhi = ((i + 0.5f) / currentRing.Count) * MathF.PI * 2f;
                    
                    // Find the two closest points in next ring
                    int nextIdx1 = FindClosestPointInRingByPhi(nextRing, currPhi);
                    int nextRingPos = GetRingPosition(nextRing, nextIdx1);
                    int nextIdx2 = nextRing[(nextRingPos + 1) % nextRing.Count];
                    
                    // Draw two triangles forming a quad
                    // Triangle 1: currIdx, currIdx2, nextIdx1
                    Color avgColor1 = AverageColor(pointColors[currIdx], pointColors[currIdx2], pointColors[nextIdx1]);
                    DrawTriangleWithBackface(
                        screenPoints[currIdx], screenPoints[currIdx2], screenPoints[nextIdx1],
                        transformed3DPoints[currIdx], transformed3DPoints[currIdx2], transformed3DPoints[nextIdx1],
                        avgColor1);
                    
                    // Triangle 2: currIdx2, nextIdx1, nextIdx2
                    Color avgColor2 = AverageColor(pointColors[currIdx2], pointColors[nextIdx1], pointColors[nextIdx2]);
                    DrawTriangleWithBackface(
                        screenPoints[currIdx2], screenPoints[nextIdx1], screenPoints[nextIdx2],
                        transformed3DPoints[currIdx2], transformed3DPoints[nextIdx1], transformed3DPoints[nextIdx2],
                        avgColor2);
                }
                
                // Also ensure all points in next ring are connected by drawing from next ring's perspective
                // This fills in any gaps when next ring has more points
                for (int i = 0; i < nextRing.Count; i++)
                {
                    int nextIdx = nextRing[i];
                    int nextIdx2 = nextRing[(i + 1) % nextRing.Count];
                    
                    // Calculate phi angle for this edge's midpoint
                    float nextPhi = ((i + 0.5f) / nextRing.Count) * MathF.PI * 2f;
                    
                    // Find the two closest points in current ring
                    int currIdx1 = FindClosestPointInRingByPhi(currentRing, nextPhi);
                    int currRingPos = GetRingPosition(currentRing, currIdx1);
                    int currIdx2 = currentRing[(currRingPos + 1) % currentRing.Count];
                    
                    // Draw triangle connecting next ring edge to current ring
                    Color avgColor = AverageColor(pointColors[nextIdx], pointColors[nextIdx2], pointColors[currIdx1]);
                    DrawTriangleWithBackface(
                        screenPoints[nextIdx], screenPoints[nextIdx2], screenPoints[currIdx1],
                        transformed3DPoints[nextIdx], transformed3DPoints[nextIdx2], transformed3DPoints[currIdx1],
                        avgColor);
                }
            }
        }
    }

    private int FindClosestPointInRingByPhi(List<int> ringIndices, float targetPhi)
    {
        int bestIdx = ringIndices[0];
        float bestDist = float.MaxValue;

        for (int i = 0; i < ringIndices.Count; i++)
        {
            int idx = ringIndices[i];
            float phi = (i / (float)ringIndices.Count) * MathF.PI * 2f;
            float dist = MathF.Abs(phi - targetPhi);
            if (dist > MathF.PI) dist = MathF.PI * 2f - dist; // Wrap around

            if (dist < bestDist)
            {
                bestDist = dist;
                bestIdx = idx;
            }
        }

        return bestIdx;
    }

    private int GetRingPosition(List<int> ringIndices, int pointIdx)
    {
        for (int i = 0; i < ringIndices.Count; i++)
        {
            if (ringIndices[i] == pointIdx)
                return i;
        }
        return 0;
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