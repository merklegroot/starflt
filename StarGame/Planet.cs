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
    
    public Planet(string name, Vector2 position, float radius, Color surfaceColor)
    {
        Name = name;
        Position = position;
        Radius = radius;
        SurfaceColor = surfaceColor;
        
        GenerateSpherePoints();
    }
    
    private void GenerateSpherePoints()
    {
        // Generate points on a unit sphere using spherical coordinates
        // Then scale by radius
        const int pointsPerRing = 20;
        const int numRings = 15;
        
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
                
                _spherePoints.Add(new Vector3(x, y, z));
            }
        }
    }
    
    public void DrawSpherePoints(Vector2 center, float displayRadius, float rotationAngle = 0.0f)
    {
        // Draw each point on the sphere
        foreach (var point in _spherePoints)
        {
            // Rotate point around Y-axis (vertical)
            float cosRot = MathF.Cos(rotationAngle);
            float sinRot = MathF.Sin(rotationAngle);
            
            float rotatedX = point.X * cosRot - point.Z * sinRot;
            float rotatedZ = point.X * sinRot + point.Z * cosRot;
            float rotatedY = point.Y;
            
            // Project 3D point to 2D screen (orthographic projection)
            // For a sphere viewed head-on, we project X and Y to screen
            float screenX = center.X + rotatedX * displayRadius;
            float screenY = center.Y + rotatedY * displayRadius;
            
            // Draw point
            Raylib.DrawPixel((int)screenX, (int)screenY, SurfaceColor);
        }
    }
}
