using Raylib_cs;
using System.Numerics;
using System;

namespace StarflightGame;

public class Planet
{
    public string Name { get; set; } = "";
    public Vector2 Position { get; set; }
    public float Radius { get; set; }
    public Color SurfaceColor { get; set; }
    private List<Vector3> _spherePoints = new List<Vector3>();
    private List<float> _pointHeights = new List<float>();
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
        // Generate points on a unit sphere using spherical coordinates
        // Then scale by radius
        const int pointsPerRing = 80;
        const int numRings = 60;
        
        for (int ring = 0; ring < numRings; ring++)
        {
            // Latitude: -π/2 to π/2 (south pole to north pole)
            float theta = (ring / (float)(numRings - 1)) * MathF.PI - MathF.PI / 2.0f;
            
            // Calculate how many points for this ring (fewer near poles)
            float ringRadius = MathF.Cos(theta);
            int pointsInRing = (int)(pointsPerRing * MathF.Abs(ringRadius));
            if (pointsInRing < 1) pointsInRing = 1;
            
            for (int point = 0; point < pointsInRing; point++)
            {
                // Longitude: 0 to 2π
                float phi = (point / (float)pointsInRing) * MathF.PI * 2.0f;
                
                // Convert spherical to Cartesian coordinates
                float x = MathF.Cos(theta) * MathF.Cos(phi);
                float y = MathF.Sin(theta);
                float z = MathF.Cos(theta) * MathF.Sin(phi);
                
                // Generate random height for this point (0.0 to 1.0, representing height multiplier)
                // Height of 1.0 = no change, > 1.0 = pushed outward, < 1.0 = pushed inward
                float height = 0.8f + (float)(_random.NextDouble() * 0.4f); // Range: 0.8 to 1.2
                
                // Track min and max heights
                if (height < _minHeight) _minHeight = height;
                if (height > _maxHeight) _maxHeight = height;
                
                _spherePoints.Add(new Vector3(x, y, z));
                _pointHeights.Add(height);
            }
        }
    }
    
    private Color GetColorForHeight(float height)
    {
        // Normalize height to 0-1 range based on min/max
        float normalizedHeight = (_maxHeight > _minHeight) 
            ? (height - _minHeight) / (_maxHeight - _minHeight)
            : 0.5f;
        
        // Map normalized height to color gradient:
        // 0.0 - 0.2: Blue (water)
        // 0.2 - 0.4: Brown (ground)
        // 0.4 - 0.6: Green (grass)
        // 0.6 - 0.8: Gray (mountain side)
        // 0.8 - 1.0: White (mountain top)
        
        byte r, g, b;
        
        if (normalizedHeight < 0.2f)
        {
            // Blue (water) - 0.0 to 0.2
            float t = normalizedHeight / 0.2f;
            r = (byte)(20 + t * 40);      // 20-60
            g = (byte)(60 + t * 40);      // 60-100
            b = (byte)(120 + t * 80);     // 120-200
        }
        else if (normalizedHeight < 0.4f)
        {
            // Brown (ground) - 0.2 to 0.4
            float t = (normalizedHeight - 0.2f) / 0.2f;
            r = (byte)(60 + t * 60);      // 60-120
            g = (byte)(100 + t * 20);     // 100-120
            b = (byte)(200 - t * 100);    // 200-100
        }
        else if (normalizedHeight < 0.6f)
        {
            // Green (grass) - 0.4 to 0.6
            float t = (normalizedHeight - 0.4f) / 0.2f;
            r = (byte)(120 - t * 60);     // 120-60
            g = (byte)(120 + t * 80);     // 120-200
            b = (byte)(100 - t * 60);     // 100-40
        }
        else if (normalizedHeight < 0.8f)
        {
            // Gray (mountain side) - 0.6 to 0.8
            float t = (normalizedHeight - 0.6f) / 0.2f;
            r = (byte)(60 + t * 80);      // 60-140
            g = (byte)(200 - t * 60);     // 200-140
            b = (byte)(40 + t * 60);      // 40-100
        }
        else
        {
            // White (mountain top) - 0.8 to 1.0
            float t = (normalizedHeight - 0.8f) / 0.2f;
            byte gray = (byte)(140 + t * 115); // 140-255
            r = gray;
            g = gray;
            b = gray;
        }
        
        return new Color(r, g, b, (byte)255);
    }
    
    public void DrawSpherePoints(Vector2 center, float displayRadius, float rotationAngle = 0.0f)
    {
        // Draw each point on the sphere
        for (int i = 0; i < _spherePoints.Count; i++)
        {
            var point = _spherePoints[i];
            float height = _pointHeights[i];
            
            // Get color based on height
            Color pointColor = GetColorForHeight(height);
            
            // Apply height to point (scale distance from center)
            Vector3 heightAdjustedPoint = point * height;
            
            // Rotate point around Y-axis (vertical)
            float cosRot = MathF.Cos(rotationAngle);
            float sinRot = MathF.Sin(rotationAngle);
            
            float rotatedX = heightAdjustedPoint.X * cosRot - heightAdjustedPoint.Z * sinRot;
            float rotatedZ = heightAdjustedPoint.X * sinRot + heightAdjustedPoint.Z * cosRot;
            float rotatedY = heightAdjustedPoint.Y;
            
            // Project 3D point to 2D screen (orthographic projection)
            // For a sphere viewed head-on, we project X and Y to screen
            float screenX = center.X + rotatedX * displayRadius;
            float screenY = center.Y + rotatedY * displayRadius;
            
            // Draw point with height-based color
            Raylib.DrawPixel((int)screenX, (int)screenY, pointColor);
        }
    }
}
