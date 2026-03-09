using Raylib_cs;
using System.Numerics;
using System;

namespace StarflightGame;

public class PlanetTerrain
{
    private readonly Planet _planet;
    private readonly TerrainGenerator _generator;
    
    // Cache terrain texture to avoid recalculating every frame
    // Disabled for now due to initialization performance - using direct rendering
    private Color[,]? _cachedTexture = null;
    private int _textureSize = 0;
    private bool _textureGenerated = false;
    private const bool USE_TEXTURE_CACHE = false; // Set to true to enable caching (slower startup)
    private const int TEXTURE_RESOLUTION = 128;
    
    public PlanetTerrain(Planet planet, int seed, float? scale = null, int? octaves = null, float? persistence = null)
    {
        _planet = planet;
        _generator = new TerrainGenerator(seed, scale, octaves, persistence);
    }
    
    private void EnsureTextureGenerated()
    {
        if (_textureGenerated) return;
        
        // Generate cached texture once in sphere coordinates (longitude/latitude)
        // Texture is stored as: X = longitude (-π to π), Y = latitude (-1 to 1)
        _textureSize = TEXTURE_RESOLUTION;
        _cachedTexture = new Color[_textureSize, _textureSize];
        
        // Generate texture mapped to sphere coordinates
        for (int latIdx = 0; latIdx < _textureSize; latIdx++)
        {
            for (int lonIdx = 0; lonIdx < _textureSize; lonIdx++)
            {
                // Map texture coordinates to sphere coordinates
                // Longitude: 0 to textureSize maps to -π to π
                float longitude = (lonIdx / (float)_textureSize) * MathF.PI * 2.0f - MathF.PI;
                // Latitude: 0 to textureSize maps to -1 to 1 (normalized)
                float latitude = (latIdx / (float)_textureSize) * 2.0f - 1.0f;
                
                // Calculate distance from center for this latitude
                // For a sphere viewed head-on, distance varies with latitude
                float latSq = Math.Clamp(latitude, -1.0f, 1.0f);
                latSq = latSq * latSq;
                float normalizedDistance = MathF.Sqrt(MathF.Max(0.001f, 1.0f - latSq));
                
                // Convert to angle for terrain sampling
                float angle = longitude;
                
                // Sample terrain
                float height = _generator.SampleHeightAtPolar(angle, normalizedDistance);
                Color terrainColor = _generator.GetTerrainColor(height, angle, normalizedDistance);
                
                // Apply edge darkening based on distance from equator
                float edgeDarkening = 1.0f - (MathF.Abs(latitude) * 0.3f);
                _cachedTexture[latIdx, lonIdx] = new Color(
                    (byte)(terrainColor.R * edgeDarkening),
                    (byte)(terrainColor.G * edgeDarkening),
                    (byte)(terrainColor.B * edgeDarkening),
                    (byte)255
                );
            }
        }
        
        _textureGenerated = true;
    }
    
    public Color GetColorAt(float angle, float distanceFromCenter)
    {
        return _generator.GetColorAt(angle, distanceFromCenter);
    }
    
    public float GetHeightAt(float angle, float distanceFromCenter)
    {
        return _generator.GetHeightAt(angle, distanceFromCenter);
    }
    
    public void DrawTerrainPixels(Vector2 center, float radius, float rotationAngle = 0.0f)
    {
        // Use direct rendering for now (texture cache disabled for performance)
        if (!USE_TEXTURE_CACHE || _cachedTexture == null)
        {
            DrawTerrainPixelsFallback(center, radius, rotationAngle);
            return;
        }
        
        int diameter = (int)(radius * 2);
        int startX = (int)(center.X - radius);
        int startY = (int)(center.Y - radius);
        
        for (int py = 0; py < diameter; py++)
        {
            for (int px = 0; px < diameter; px++)
            {
                float x = px - radius;
                float y = py - radius;
                float distance = MathF.Sqrt(x * x + y * y);
                
                if (distance > radius) continue;
                
                // Normalize to unit circle
                float normalizedX = x / radius;
                float normalizedY = y / radius;
                
                // Proper 3D sphere rotation: rotate around Y-axis (vertical)
                float xyDistanceSq = normalizedX * normalizedX + normalizedY * normalizedY;
                if (xyDistanceSq >= 1.0f) continue;
                
                // Calculate Z on sphere
                float z = MathF.Sqrt(1.0f - xyDistanceSq);
                
                // Rotate around Y-axis
                float cosRot = MathF.Cos(rotationAngle);
                float sinRot = MathF.Sin(rotationAngle);
                float rotatedX = normalizedX * cosRot - z * sinRot;
                float rotatedZ = normalizedX * sinRot + z * cosRot;
                
                // Convert rotated 3D point to sphere coordinates
                float longitude = MathF.Atan2(rotatedX, rotatedZ);
                float latitude = normalizedY; // Y stays the same for vertical axis rotation
                
                // Map sphere coordinates to texture coordinates
                // Longitude: -π to π maps to 0 to textureSize (wraps around)
                float texU = ((longitude + MathF.PI) / (MathF.PI * 2.0f)) * _textureSize;
                // Latitude: -1 to 1 maps to 0 to textureSize
                float texV = ((latitude + 1.0f) * 0.5f) * _textureSize;
                
                // Wrap longitude (U coordinate)
                texU = texU % _textureSize;
                if (texU < 0) texU += _textureSize;
                
                // Clamp coordinates
                texU = Math.Clamp(texU, 0, _textureSize - 1);
                texV = Math.Clamp(texV, 0, _textureSize - 1);
                
                // Sample from cached texture
                int texX = (int)texU;
                int texY = (int)texV;
                Color terrainColor = _cachedTexture[texY, texX];
                
                Raylib.DrawPixel(startX + px, startY + py, terrainColor);
            }
        }
    }
    
    private void DrawTerrainPixelsFallback(Vector2 center, float radius, float rotationAngle = 0.0f)
    {
        // Fallback method: draw directly without cache (slower but works)
        int diameter = (int)(radius * 2);
        int startX = (int)(center.X - radius);
        int startY = (int)(center.Y - radius);
        
        for (int py = 0; py < diameter; py++)
        {
            for (int px = 0; px < diameter; px++)
            {
                float x = px - radius;
                float y = py - radius;
                float distance = MathF.Sqrt(x * x + y * y);
                
                if (distance > radius) continue;
                
                float normalizedX = x / radius;
                float normalizedY = y / radius;
                float normalizedDistance = distance / radius;
                
                // 3D sphere rotation
                float xyDistanceSq = normalizedX * normalizedX + normalizedY * normalizedY;
                if (xyDistanceSq >= 1.0f) continue;
                
                float z = MathF.Sqrt(1.0f - xyDistanceSq);
                float cosRot = MathF.Cos(rotationAngle);
                float sinRot = MathF.Sin(rotationAngle);
                float rotatedX = normalizedX * cosRot - z * sinRot;
                float rotatedZ = normalizedX * sinRot + z * cosRot;
                
                float longitude = MathF.Atan2(rotatedX, rotatedZ);
                float angle = longitude;
                
                float height = _generator.SampleHeightAtPolar(angle, normalizedDistance);
                Color terrainColor = _generator.GetTerrainColor(height, angle, normalizedDistance);
                
                float edgeDarkening = 1.0f - (normalizedDistance * 0.3f);
                terrainColor = new Color(
                    (byte)(terrainColor.R * edgeDarkening),
                    (byte)(terrainColor.G * edgeDarkening),
                    (byte)(terrainColor.B * edgeDarkening),
                    (byte)255
                );
                
                Raylib.DrawPixel(startX + px, startY + py, terrainColor);
            }
        }
    }
}
