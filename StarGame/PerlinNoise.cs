using System;

namespace StarflightGame;

/// <summary>
/// Simple Perlin noise implementation for procedural terrain generation
/// </summary>
public class PerlinNoise
{
    private readonly int[] _permutation;
    private readonly Random _random;

    public PerlinNoise(int seed)
    {
        _random = new Random(seed);
        _permutation = new int[512];
        
        // Initialize permutation array
        int[] p = new int[256];
        for (int i = 0; i < 256; i++)
        {
            p[i] = i;
        }
        
        // Shuffle
        for (int i = 255; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            int temp = p[i];
            p[i] = p[j];
            p[j] = temp;
        }
        
        // Duplicate for wrapping
        for (int i = 0; i < 512; i++)
        {
            _permutation[i] = p[i % 256];
        }
    }

    public float Noise(float x, float y)
    {
        // Find unit grid cell containing point
        int X = (int)Math.Floor(x) & 255;
        int Y = (int)Math.Floor(y) & 255;

        // Get relative x,y coordinates of point within that cell
        x -= (float)Math.Floor(x);
        y -= (float)Math.Floor(y);

        // Compute fade curves for each of x, y
        float u = Fade(x);
        float v = Fade(y);

        // Hash coordinates of the 4 square corners
        int A = _permutation[X] + Y;
        int AA = _permutation[A];
        int AB = _permutation[A + 1];
        int B = _permutation[X + 1] + Y;
        int BA = _permutation[B];
        int BB = _permutation[B + 1];

        // And add blended results from 4 corners of the square
        return Lerp(v,
            Lerp(u, Grad(_permutation[AA], x, y),
                Grad(_permutation[BA], x - 1, y)),
            Lerp(u, Grad(_permutation[AB], x, y - 1),
                Grad(_permutation[BB], x - 1, y - 1)));
    }

    public float OctaveNoise(float x, float y, int octaves, float persistence = 0.5f, float scale = 1.0f)
    {
        float total = 0;
        float frequency = scale;
        float amplitude = 1;
        float maxValue = 0;

        for (int i = 0; i < octaves; i++)
        {
            total += Noise(x * frequency, y * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= 2;
        }

        return total / maxValue;
    }

    private static float Fade(float t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    private static float Lerp(float t, float a, float b)
    {
        return a + t * (b - a);
    }

    private static float Grad(int hash, float x, float y)
    {
        int h = hash & 3;
        float u = h < 2 ? x : y;
        float v = h < 2 ? y : x;
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }
}
