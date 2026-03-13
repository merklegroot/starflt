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
    private List<List<int>> _ringIndices = new List<List<int>>(); // Store point indices for each ring
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
        // Organize by rings for triangle generation
        const int pointsPerRing = 80;
        const int numRings = 60;
        
        // First, generate all the sphere points
        for (int ring = 0; ring < numRings; ring++)
        {
            // Latitude: -π/2 to π/2 (south pole to north pole)
            float theta = (ring / (float)(numRings - 1)) * MathF.PI - MathF.PI / 2.0f;
            
            // Calculate how many points for this ring (fewer near poles)
            float ringRadius = MathF.Cos(theta);
            int pointsInRing = (int)(pointsPerRing * MathF.Abs(ringRadius));
            if (pointsInRing < 1) pointsInRing = 1;
            
            List<int> ringPointIndices = new List<int>();
            
            for (int point = 0; point < pointsInRing; point++)
            {
                // Longitude: 0 to 2π
                float phi = (point / (float)pointsInRing) * MathF.PI * 2.0f;
                
                // Convert spherical to Cartesian coordinates
                float x = MathF.Cos(theta) * MathF.Cos(phi);
                float y = MathF.Sin(theta);
                float z = MathF.Cos(theta) * MathF.Sin(phi);
                
                int pointIndex = _spherePoints.Count;
                _spherePoints.Add(new Vector3(x, y, z));
                _pointHeights.Add(1.0f); // Placeholder, will be filled by diamond-square
                ringPointIndices.Add(pointIndex);
            }
            
            _ringIndices.Add(ringPointIndices);
        }
        
        // Now generate heights using diamond-square algorithm adapted for sphere
        GenerateDiamondSquareHeights(numRings, pointsPerRing);
    }
    
    private void GenerateDiamondSquareHeights(int numRings, int pointsPerRing)
    {
        // Use a fixed power-of-2 grid for diamond-square algorithm
        // This makes subdivision easier and more efficient
        int gridSize = 64; // Power of 2 for clean subdivision
        float[,] heightMap = new float[gridSize, gridSize];
        
        // Initialize height map with base value
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                heightMap[i, j] = 1.0f; // Base height
            }
        }
        
        // Initialize corners with random values
        // IMPORTANT: For wrap-around, left edge (0) and right edge (gridSize-1) must match
        // Initialize top-left corner (will also be top-right due to wrap-around)
        float topLeft = 0.9f + (float)(_random.NextDouble() * 0.2f);
        heightMap[0, 0] = topLeft;
        heightMap[0, gridSize - 1] = topLeft; // Same value for wrap-around
        
        // Initialize bottom-left corner (will also be bottom-right due to wrap-around)
        float bottomLeft = 0.9f + (float)(_random.NextDouble() * 0.2f);
        heightMap[gridSize - 1, 0] = bottomLeft;
        heightMap[gridSize - 1, gridSize - 1] = bottomLeft; // Same value for wrap-around
        
        // Perform diamond-square subdivision
        float roughness = 0.4f; // Controls terrain roughness
        DiamondSquareSubdivide(heightMap, 0, gridSize - 1, 0, gridSize - 1, roughness);
        
        // CRITICAL: Ensure wrap-around continuity - column 0 and gridSize-1 must match for all rows
        // This fixes any discontinuities that might have been introduced during subdivision
        for (int i = 0; i < gridSize; i++)
        {
            // Average the values at the seam to ensure smooth transition
            float avg = (heightMap[i, 0] + heightMap[i, gridSize - 1]) / 2.0f;
            heightMap[i, 0] = avg;
            heightMap[i, gridSize - 1] = avg;
        }
        
        // Map height map to sphere points using spherical coordinates
        for (int ring = 0; ring < _ringIndices.Count; ring++)
        {
            var ringPoints = _ringIndices[ring];
            int pointsInRing = ringPoints.Count;
            
            // Calculate latitude for this ring (-π/2 to π/2)
            float theta = (ring / (float)(numRings - 1)) * MathF.PI - MathF.PI / 2.0f;
            
            // Map latitude to height map row (0 to gridSize - 1)
            // Use normalized latitude: 0 = south pole, 1 = north pole
            float normalizedLat = (theta + MathF.PI / 2.0f) / MathF.PI;
            float heightMapRow = normalizedLat * (gridSize - 1);
            
            for (int point = 0; point < pointsInRing; point++)
            {
                // Calculate longitude for this point (0 to 2π)
                float phi = (point / (float)pointsInRing) * MathF.PI * 2.0f;
                
                // Map longitude to height map column (0 to gridSize - 1)
                float normalizedLon = phi / (MathF.PI * 2.0f);
                float heightMapCol = normalizedLon * gridSize;
                
                // Sample height from height map (with bilinear interpolation for smoother results)
                float height = SampleHeightMap(heightMap, heightMapRow, heightMapCol, gridSize);
                
                // Clamp height to reasonable range
                height = MathF.Max(0.7f, MathF.Min(1.3f, height));
                
                int pointIndex = ringPoints[point];
                _pointHeights[pointIndex] = height;
                
                // Track min and max heights
                if (height < _minHeight) _minHeight = height;
                if (height > _maxHeight) _maxHeight = height;
            }
        }
    }
    
    private float SampleHeightMap(float[,] heightMap, float row, float col, int gridSize)
    {
        // Use bilinear interpolation for smoother sampling
        // This helps reduce discontinuities at equator and other boundaries
        
        // Clamp row
        int row0 = Math.Max(0, Math.Min(gridSize - 1, (int)row));
        int row1 = Math.Max(0, Math.Min(gridSize - 1, row0 + 1));
        
        // Handle column wrap-around
        int colInt = (int)col;
        int col0 = colInt % gridSize;
        if (col0 < 0) col0 += gridSize;
        // Treat gridSize-1 as 0 for wrap-around
        if (col0 == gridSize - 1) col0 = 0;
        int col1 = (col0 + 1) % gridSize;
        if (col1 == gridSize - 1) col1 = 0;
        
        // Get the four surrounding points using wrap-around aware access
        float h00 = GetHeightWithWrapAround(heightMap, row0, col0, gridSize);
        float h01 = GetHeightWithWrapAround(heightMap, row0, col1, gridSize);
        float h10 = GetHeightWithWrapAround(heightMap, row1, col0, gridSize);
        float h11 = GetHeightWithWrapAround(heightMap, row1, col1, gridSize);
        
        // Calculate fractional parts for interpolation
        float rowFrac = row - row0;
        float colFrac = col - colInt;
        if (colFrac < 0) colFrac += gridSize;
        colFrac = colFrac % 1.0f;
        
        // Bilinear interpolation
        float h0 = h00 * (1.0f - colFrac) + h01 * colFrac;
        float h1 = h10 * (1.0f - colFrac) + h11 * colFrac;
        return h0 * (1.0f - rowFrac) + h1 * rowFrac;
    }
    
    private float GetHeightWithWrapAround(float[,] heightMap, int x, int y, int gridSize)
    {
        // Get height value, ensuring wrap-around continuity
        // Column 0 and gridSize-1 are treated as the same point
        int yNorm = y % gridSize;
        if (yNorm < 0) yNorm += gridSize;
        
        // If accessing column gridSize-1, read from column 0 instead (they're the same)
        if (yNorm == gridSize - 1)
        {
            yNorm = 0;
        }
        
        return heightMap[x, yNorm];
    }
    
    private void SetHeightWithWrapAround(float[,] heightMap, int x, int y, float value, int gridSize)
    {
        // Set height value, ensuring wrap-around continuity
        // Column 0 and gridSize-1 are treated as the same point
        int yNorm = y % gridSize;
        if (yNorm < 0) yNorm += gridSize;
        
        // If setting column gridSize-1, set column 0 instead (they're the same)
        if (yNorm == gridSize - 1)
        {
            yNorm = 0;
        }
        
        heightMap[x, yNorm] = value;
        // Also set gridSize-1 to maintain the array structure
        heightMap[x, gridSize - 1] = value;
    }
    
    private void DiamondSquareSubdivide(float[,] heightMap, int x1, int x2, int y1, int y2, float roughness)
    {
        int gridSize = heightMap.GetLength(1);
        int width = x2 - x1;
        
        // Normalize y coordinates to handle wrap-around (longitude wraps at 0 = gridSize)
        int y1Norm = y1 % gridSize;
        if (y1Norm < 0) y1Norm += gridSize;
        int y2Norm = y2 % gridSize;
        if (y2Norm < 0) y2Norm += gridSize;
        
        // Calculate actual height, accounting for wrap-around
        int height = y2Norm - y1Norm;
        if (height < 0) height += gridSize;
        if (height == 0) height = gridSize; // Full wrap-around
        
        // Base case: stop when region is too small
        if (width < 2 && height < 2)
        {
            return;
        }
        
        int xMid = (x1 + x2) / 2;
        
        // Calculate yMid, handling wrap-around properly
        int yMid;
        if (y1Norm <= y2Norm)
        {
            yMid = (y1Norm + y2Norm) / 2;
        }
        else
        {
            // Wrap-around case: we're crossing the seam (0/gridSize-1 boundary)
            // Calculate midpoint accounting for wrap-around
            int dist1 = gridSize - y1Norm; // Distance from y1Norm to gridSize-1
            int dist2 = y2Norm; // Distance from 0 to y2Norm
            int totalDist = dist1 + dist2;
            yMid = (y1Norm + totalDist / 2) % gridSize;
        }
        
        // Diamond step: set center point to average of corners plus random offset
        if (width >= 2 && height >= 2)
        {
            float avg = (GetHeightWithWrapAround(heightMap, x1, y1Norm, gridSize) + 
                         GetHeightWithWrapAround(heightMap, x1, y2Norm, gridSize) + 
                         GetHeightWithWrapAround(heightMap, x2, y1Norm, gridSize) + 
                         GetHeightWithWrapAround(heightMap, x2, y2Norm, gridSize)) / 4.0f;
            float offset = (float)(_random.NextDouble() * 2.0 - 1.0) * roughness;
            SetHeightWithWrapAround(heightMap, xMid, yMid, avg + offset, gridSize);
        }
        
        // Square step: set edge midpoints
        // Top edge (x1) - horizontal edge
        if (height >= 2)
        {
            float avg = (GetHeightWithWrapAround(heightMap, x1, y1Norm, gridSize) + 
                         GetHeightWithWrapAround(heightMap, x1, y2Norm, gridSize)) / 2.0f;
            if (width >= 2)
            {
                avg = (avg + GetHeightWithWrapAround(heightMap, xMid, yMid, gridSize)) / 2.0f;
            }
            float offset = (float)(_random.NextDouble() * 2.0 - 1.0) * roughness;
            SetHeightWithWrapAround(heightMap, x1, yMid, avg + offset, gridSize);
        }
        
        // Bottom edge (x2) - horizontal edge
        if (height >= 2 && width >= 2)
        {
            float avg = (GetHeightWithWrapAround(heightMap, x2, y1Norm, gridSize) + 
                         GetHeightWithWrapAround(heightMap, x2, y2Norm, gridSize)) / 2.0f;
            avg = (avg + GetHeightWithWrapAround(heightMap, xMid, yMid, gridSize)) / 2.0f;
            float offset = (float)(_random.NextDouble() * 2.0 - 1.0) * roughness;
            SetHeightWithWrapAround(heightMap, x2, yMid, avg + offset, gridSize);
        }
        
        // Left edge (y1) - vertical edge, ensure wrap-around continuity
        if (width >= 2)
        {
            float avg = (GetHeightWithWrapAround(heightMap, x1, y1Norm, gridSize) + 
                         GetHeightWithWrapAround(heightMap, x2, y1Norm, gridSize)) / 2.0f;
            if (height >= 2)
            {
                avg = (avg + GetHeightWithWrapAround(heightMap, xMid, yMid, gridSize)) / 2.0f;
            }
            float offset = (float)(_random.NextDouble() * 2.0 - 1.0) * roughness;
            SetHeightWithWrapAround(heightMap, xMid, y1Norm, avg + offset, gridSize);
        }
        
        // Right edge (y2) - vertical edge, ensure wrap-around continuity
        if (width >= 2)
        {
            float avg = (GetHeightWithWrapAround(heightMap, x1, y2Norm, gridSize) + 
                         GetHeightWithWrapAround(heightMap, x2, y2Norm, gridSize)) / 2.0f;
            if (height >= 2)
            {
                avg = (avg + GetHeightWithWrapAround(heightMap, xMid, yMid, gridSize)) / 2.0f;
            }
            float offset = (float)(_random.NextDouble() * 2.0 - 1.0) * roughness;
            SetHeightWithWrapAround(heightMap, xMid, y2Norm, avg + offset, gridSize);
        }
        
        // Recursively subdivide the four quadrants
        float newRoughness = roughness * 0.6f; // Reduce roughness with each subdivision
        
        if (width >= 2 && height >= 2)
        {
            // Subdivide quadrants, handling wrap-around in y dimension
            DiamondSquareSubdivide(heightMap, x1, xMid, y1Norm, yMid, newRoughness);
            DiamondSquareSubdivide(heightMap, x1, xMid, yMid, y2Norm, newRoughness);
            DiamondSquareSubdivide(heightMap, xMid, x2, y1Norm, yMid, newRoughness);
            DiamondSquareSubdivide(heightMap, xMid, x2, yMid, y2Norm, newRoughness);
        }
        else if (width >= 2)
        {
            DiamondSquareSubdivide(heightMap, x1, xMid, y1Norm, y2Norm, newRoughness);
            DiamondSquareSubdivide(heightMap, xMid, x2, y1Norm, y2Norm, newRoughness);
        }
        else if (height >= 2)
        {
            DiamondSquareSubdivide(heightMap, x1, x2, y1Norm, yMid, newRoughness);
            DiamondSquareSubdivide(heightMap, x1, x2, yMid, y2Norm, newRoughness);
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
    
    private Vector2 Project3DTo2D(Vector3 point3D, Vector2 center, float displayRadius)
    {
        // Project 3D point to 2D screen (orthographic projection)
        // For a sphere viewed head-on, we project X and Y to screen
        return new Vector2(
            center.X + point3D.X * displayRadius,
            center.Y + point3D.Y * displayRadius
        );
    }
    
    private Vector3 TransformPoint(Vector3 point, float height, float rotationAngle)
    {
        // Apply height to point (scale distance from center)
        Vector3 heightAdjustedPoint = point * height;
        
        // Rotate point around Y-axis (vertical)
        float cosRot = MathF.Cos(rotationAngle);
        float sinRot = MathF.Sin(rotationAngle);
        
        return new Vector3(
            heightAdjustedPoint.X * cosRot - heightAdjustedPoint.Z * sinRot,
            heightAdjustedPoint.Y,
            heightAdjustedPoint.X * sinRot + heightAdjustedPoint.Z * cosRot
        );
    }
    
    public void DrawSpherePoints(Vector2 center, float displayRadius, float rotationAngle = 0.0f)
    {
        // Draw individual points
        for (int i = 0; i < _spherePoints.Count; i++)
        {
            Vector3 point3D = TransformPoint(_spherePoints[i], _pointHeights[i], rotationAngle);
            Vector2 screenPos = Project3DTo2D(point3D, center, displayRadius);
            Color color = GetColorForHeight(_pointHeights[i]);
            
            Raylib.DrawCircleV(screenPos, 2.0f, color);
        }
    }
}
