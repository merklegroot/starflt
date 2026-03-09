using Raylib_cs;
using System;

namespace StarflightGame;

public class TerrainGenerator
{
    private readonly PerlinNoise _noise;
    private readonly int _seed;
    private readonly float _scale;
    private readonly int _octaves;
    private readonly float _persistence;
    
    public TerrainGenerator(int seed, float? scale = null, int? octaves = null, float? persistence = null)
    {
        _seed = seed;
        _noise = new PerlinNoise(seed);
        
        // Use provided parameters or generate varied ones based on seed
        Random paramRandom = new Random(seed);
        _scale = scale ?? (0.05f + (float)(paramRandom.NextDouble() * 0.15f)); // 0.05 to 0.2
        _octaves = octaves ?? (3 + paramRandom.Next(3)); // 3 to 5
        _persistence = persistence ?? (0.4f + (float)(paramRandom.NextDouble() * 0.4f)); // 0.4 to 0.8
    }
    
    public float SampleHeightAtPolar(float angle, float radius)
    {
        // Convert polar coordinates to 2D noise space
        // Use angle and radius directly to avoid distortion
        float scale = _scale;
        int octaves = _octaves;
        float persistence = _persistence;
        
        // Map polar coordinates to noise space
        // Angle wraps around naturally, radius goes from 0 to 1
        float noiseX = MathF.Cos(angle) * radius * scale;
        float noiseY = MathF.Sin(angle) * radius * scale;
        
        // Add a second coordinate system offset to break up patterns
        float noiseX2 = MathF.Cos(angle + MathF.PI * 0.5f) * radius * scale * 0.7f;
        float noiseY2 = MathF.Sin(angle + MathF.PI * 0.5f) * radius * scale * 0.7f;
        
        // Combine both coordinate systems for more natural patterns
        float sampleX = noiseX + noiseX2;
        float sampleY = noiseY + noiseY2;
        
        // Generate primary height using octave noise
        float height = _noise.OctaveNoise(sampleX, sampleY, octaves, persistence, 1.0f);
        
        // Add additional terrain features using different noise scales
        // Large scale features (continents/regions)
        float largeScale = _noise.OctaveNoise(sampleX * 0.3f, sampleY * 0.3f, 2, 0.7f, 1.0f) * 0.3f;
        
        // Medium scale features (mountains/valleys)
        float mediumScale = _noise.OctaveNoise(sampleX * 1.5f, sampleY * 1.5f, 3, 0.6f, 1.0f) * 0.4f;
        
        // Fine detail
        float fineDetail = _noise.Noise(sampleX * 4.0f, sampleY * 4.0f) * 0.3f;
        
        // Combine layers
        height = height * 0.5f + largeScale + mediumScale + fineDetail;
        
        // Normalize to 0-1 range
        return Math.Clamp((height + 1.0f) * 0.5f, 0.0f, 1.0f);
    }
    
    public Color GetTerrainColor(float height, float angle, float radius)
    {
        // Earth-like color scheme based on height
        float r, g, b;
        
        // Add some noise for texture variation using polar coordinates
        float noiseX = MathF.Cos(angle) * radius * 3.0f;
        float noiseY = MathF.Sin(angle) * radius * 3.0f;
        float textureNoise = _noise.Noise(noiseX, noiseY) * 0.1f;
        
        float detailX = MathF.Cos(angle + 0.5f) * radius * 8.0f;
        float detailY = MathF.Sin(angle + 0.5f) * radius * 8.0f;
        float detailNoise = _noise.Noise(detailX, detailY) * 0.05f;
        float combinedTexture = textureNoise + detailNoise;
        
        if (height > 0.85f)
        {
            // Very high elevation - Snow-capped peaks (white with slight blue tint)
            r = 240 + combinedTexture * 15;
            g = 240 + combinedTexture * 15;
            b = 250 + combinedTexture * 5;
        }
        else if (height > 0.65f)
        {
            // High elevation - Mountain peaks (brown/gray)
            r = 120 + combinedTexture * 30;
            g = 100 + combinedTexture * 25;
            b = 90 + combinedTexture * 20;
            
            // Add some variation between brown and gray
            float brownX = MathF.Cos(angle) * radius * 1.5f;
            float brownY = MathF.Sin(angle) * radius * 1.5f;
            float brownVariation = _noise.Noise(brownX, brownY) * 0.2f;
            r += brownVariation * 40;
            g += brownVariation * 30;
            b += brownVariation * 20;
        }
        else if (height > 0.45f)
        {
            // Mid-high elevation - Hills/mountain bases (darker brown)
            r = 100 + combinedTexture * 25;
            g = 80 + combinedTexture * 20;
            b = 60 + combinedTexture * 15;
            
            // Blend towards green-brown
            float greenX = MathF.Cos(angle) * radius * 2.0f;
            float greenY = MathF.Sin(angle) * radius * 2.0f;
            float greenBlend = _noise.Noise(greenX, greenY) * 0.3f;
            g += greenBlend * 30;
        }
        else if (height > 0.35f)
        {
            // Mid elevation - Land/plains (green-brown mix)
            r = 60 + combinedTexture * 20;
            g = 100 + combinedTexture * 30;
            b = 40 + combinedTexture * 15;
            
            // Add variation between green and brown
            float greenVarX = MathF.Cos(angle) * radius * 1.8f;
            float greenVarY = MathF.Sin(angle) * radius * 1.8f;
            float greenVariation = _noise.Noise(greenVarX, greenVarY) * 0.4f;
            if (greenVariation > 0)
            {
                // More green (forests/grasslands)
                g += greenVariation * 50;
                r += greenVariation * 20;
            }
            else
            {
                // More brown (drier areas)
                r += Math.Abs(greenVariation) * 40;
                g += Math.Abs(greenVariation) * 20;
            }
        }
        else if (height > 0.25f)
        {
            // Low-mid elevation - Coastal/shallow areas (green-blue mix)
            r = 40 + combinedTexture * 15;
            g = 80 + combinedTexture * 25;
            b = 100 + combinedTexture * 30;
            
            // Blend from green to blue
            float blueX = MathF.Cos(angle) * radius * 2.5f;
            float blueY = MathF.Sin(angle) * radius * 2.5f;
            float blueBlend = _noise.Noise(blueX, blueY) * 0.3f;
            if (blueBlend > 0)
            {
                b += blueBlend * 40;
                g += blueBlend * 20;
            }
        }
        else
        {
            // Low elevation - Ocean (blue)
            r = 20 + combinedTexture * 10;
            g = 60 + combinedTexture * 20;
            b = 120 + combinedTexture * 35;
            
            // Add depth variation (deeper = darker blue)
            float depthX = MathF.Cos(angle) * radius * 1.2f;
            float depthY = MathF.Sin(angle) * radius * 1.2f;
            float depthVariation = _noise.Noise(depthX, depthY) * 0.2f;
            if (depthVariation < 0)
            {
                // Deeper water - darker blue
                r = Math.Max(10, r + depthVariation * 10);
                g = Math.Max(40, g + depthVariation * 15);
                b = Math.Max(80, b + depthVariation * 25);
            }
            else
            {
                // Shallower water - lighter blue-green
                r += depthVariation * 15;
                g += depthVariation * 25;
                b += depthVariation * 20;
            }
        }
        
        // Clamp values
        r = Math.Clamp(r, 0, 255);
        g = Math.Clamp(g, 0, 255);
        b = Math.Clamp(b, 0, 255);
        
        return new Color((byte)r, (byte)g, (byte)b, (byte)255);
    }
    
    public Color GetColorAt(float angle, float distanceFromCenter)
    {
        // Sample directly in polar coordinates
        float height = SampleHeightAtPolar(angle, distanceFromCenter);
        return GetTerrainColor(height, angle, distanceFromCenter);
    }
    
    public float GetHeightAt(float angle, float distanceFromCenter)
    {
        // Sample directly in polar coordinates
        return SampleHeightAtPolar(angle, distanceFromCenter);
    }
}
