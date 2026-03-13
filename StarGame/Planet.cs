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

    public void DrawSpherePoints(Vector2 center, float displayRadius, float rotationAngle = 0f)
    {
        for (int i = 0; i < _spherePoints.Count; i++)
        {
            Vector3 p = TransformPoint(_spherePoints[i], _pointHeights[i], rotationAngle);
            Vector2 screen = Project3DTo2D(p, center, displayRadius);
            Color col = GetColorForHeight(_pointHeights[i]);

            Raylib.DrawCircleV(screen, 2f, col);
        }
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