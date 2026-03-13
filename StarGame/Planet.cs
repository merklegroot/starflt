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

    private List<Vector3> _spherePoints = new List<Vector3>();
    private List<float> _pointHeights = new List<float>();
    private List<List<int>> _ringIndices = new List<List<int>>();
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
        const int gridSize = 65; // 64 + 1 for diamond-square

        for (int ring = 0; ring < numRings; ring++)
        {
            float theta = (ring / (float)(numRings - 1)) * MathF.PI - MathF.PI / 2.0f;
            float ringRadius = MathF.Cos(theta);
            int pointsInRing = (int)(pointsPerRing * MathF.Abs(ringRadius));
            if (pointsInRing < 3) pointsInRing = 3;

            if (ring == 0 || ring == numRings - 1) pointsInRing = 1; // poles

            List<int> ringPointIndices = new List<int>();

            for (int point = 0; point < pointsInRing; point++)
            {
                float phi = (point / (float)pointsInRing) * MathF.PI * 2.0f;

                float x = ringRadius * MathF.Cos(phi);
                float y = MathF.Sin(theta);
                float z = ringRadius * MathF.Sin(phi);

                int pointIndex = _spherePoints.Count;
                _spherePoints.Add(new Vector3(x, y, z));
                _pointHeights.Add(1.0f);
                ringPointIndices.Add(pointIndex);
            }

            _ringIndices.Add(ringPointIndices);
        }

        GenerateDiamondSquareHeights(numRings, pointsPerRing, gridSize);
    }

    private void GenerateDiamondSquareHeights(int numRings, int pointsPerRing, int gridSize)
    {
        float[,] heightMap = new float[gridSize, gridSize];

        // Base height
        for (int i = 0; i < gridSize; i++)
            for (int j = 0; j < gridSize; j++)
                heightMap[i, j] = 1.0f;

        // Seed corners - left == right
        float topLeft = 0.9f + (float)(_random.NextDouble() * 0.4f); // wider for more variation
        heightMap[0, 0] = topLeft;
        heightMap[0, gridSize - 1] = topLeft;

        float bottomLeft = 0.9f + (float)(_random.NextDouble() * 0.4f);
        heightMap[gridSize - 1, 0] = bottomLeft;
        heightMap[gridSize - 1, gridSize - 1] = bottomLeft;

        // Iterative diamond-square
        DiamondSquareIterative(heightMap, initialRoughness: 0.6f);

        // Sample to points
        for (int ring = 0; ring < _ringIndices.Count; ring++)
        {
            var ringPoints = _ringIndices[ring];
            int pointsInRing = ringPoints.Count;

            float theta = (ring / (float)(numRings - 1)) * MathF.PI - MathF.PI / 2.0f;
            float normalizedLat = (theta + MathF.PI / 2.0f) / MathF.PI;
            float heightMapRow = normalizedLat * (gridSize - 1);

            for (int point = 0; point < pointsInRing; point++)
            {
                float phi = (point / (float)pointsInRing) * MathF.PI * 2.0f;
                float normalizedLon = phi / (MathF.PI * 2.0f);
                float heightMapCol = normalizedLon * (gridSize - 1);

                float height = SampleHeightMap(heightMap, heightMapRow, heightMapCol, gridSize);
                height = MathF.Max(0.7f, MathF.Min(1.4f, height));

                int pointIndex = ringPoints[point];
                _pointHeights[pointIndex] = height;

                if (height < _minHeight) _minHeight = height;
                if (height > _maxHeight) _maxHeight = height;
            }
        }
    }

    private void DiamondSquareIterative(float[,] heightMap, float initialRoughness)
    {
        int size = heightMap.GetLength(0);
        float roughness = initialRoughness;

        for (int step = size - 1; step >= 2; step /= 2)
        {
            int half = step / 2;

            // Diamond step
            for (int y = 0; y < size - 1; y += step)
            {
                for (int x = 0; x < size - 1; x += step)
                {
                    float avg = (
                        heightMap[x, y] +
                        heightMap[x, (y + step) % (size - 1)] +
                        heightMap[(x + step) % (size - 1), y] +
                        heightMap[(x + step) % (size - 1), (y + step) % (size - 1)]
                    ) / 4f;

                    int mx = (x + half) % (size - 1);
                    int my = (y + half) % (size - 1);

                    heightMap[mx, my] = avg + ((float)_random.NextDouble() * 2 - 1) * roughness;

                    if (my == 0) heightMap[mx, size - 1] = heightMap[mx, my];
                }
            }

            // Square step
            for (int y = 0; y < size; y += step)
            {
                for (int x = 0; x < size; x += step)
                {
                    int mx = (x + half) % (size - 1);
                    int my = (y + half) % (size - 1);

                    float hAvg = (
                        heightMap[x % (size - 1), y % (size - 1)] +
                        heightMap[(x + step) % (size - 1), y % (size - 1)]
                    ) / 2f;

                    float vAvg = (
                        heightMap[x % (size - 1), (y + step) % (size - 1)] +
                        heightMap[x % (size - 1), (y - step + (size - 1)) % (size - 1)]
                    ) / 2f;

                    float avg = (hAvg + vAvg) / 2f;
                    heightMap[mx, my] = avg + ((float)_random.NextDouble() * 2 - 1) * roughness * 0.8f;

                    if (my == 0) heightMap[mx, size - 1] = heightMap[mx, my];
                }
            }

            roughness *= 0.55f;
        }
    }

    private float SampleHeightMap(float[,] heightMap, float row, float col, int gridSize)
    {
        int row0 = Math.Clamp((int)row, 0, gridSize - 1);
        int row1 = Math.Clamp(row0 + 1, 0, gridSize - 1);

        float colF = col % (gridSize - 1);
        if (colF < 0) colF += (gridSize - 1);

        int col0 = (int)Math.Floor(colF);
        int col1 = (col0 + 1) % (gridSize - 1);

        float h00 = heightMap[row0, col0];
        float h01 = heightMap[row0, col1];
        float h10 = heightMap[row1, col0];
        float h11 = heightMap[row1, col1];

        float rowFrac = row - row0;
        float colFrac = colF - col0;

        float h0 = h00 * (1 - colFrac) + h01 * colFrac;
        float h1 = h10 * (1 - colFrac) + h11 * colFrac;
        return h0 * (1 - rowFrac) + h1 * rowFrac;
    }

    private Color GetColorForHeight(float height)
    {
        float normalizedHeight = (_maxHeight > _minHeight)
            ? (height - _minHeight) / (_maxHeight - _minHeight)
            : 0.5f;

        byte r, g, b;

        if (normalizedHeight < 0.2f)
        {
            float t = normalizedHeight / 0.2f;
            r = (byte)(20 + t * 40);
            g = (byte)(60 + t * 40);
            b = (byte)(120 + t * 80);
        }
        else if (normalizedHeight < 0.4f)
        {
            float t = (normalizedHeight - 0.2f) / 0.2f;
            r = (byte)(60 + t * 60);
            g = (byte)(100 + t * 20);
            b = (byte)(200 - t * 100);
        }
        else if (normalizedHeight < 0.6f)
        {
            float t = (normalizedHeight - 0.4f) / 0.2f;
            r = (byte)(120 - t * 60);
            g = (byte)(120 + t * 80);
            b = (byte)(100 - t * 60);
        }
        else if (normalizedHeight < 0.8f)
        {
            float t = (normalizedHeight - 0.6f) / 0.2f;
            r = (byte)(60 + t * 80);
            g = (byte)(200 - t * 60);
            b = (byte)(40 + t * 60);
        }
        else
        {
            float t = (normalizedHeight - 0.8f) / 0.2f;
            byte gray = (byte)(140 + t * 115);
            r = gray;
            g = gray;
            b = gray;
        }

        return new Color(r, g, b, (byte)255);
    }

    private Vector2 Project3DTo2D(Vector3 point3D, Vector2 center, float displayRadius)
    {
        return new Vector2(
            center.X + point3D.X * displayRadius,
            center.Y + point3D.Y * displayRadius
        );
    }

    private Vector3 TransformPoint(Vector3 point, float height, float rotationAngle)
    {
        Vector3 heightAdjustedPoint = point * height;

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
        for (int i = 0; i < _spherePoints.Count; i++)
        {
            Vector3 point3D = TransformPoint(_spherePoints[i], _pointHeights[i], rotationAngle);
            Vector2 screenPos = Project3DTo2D(point3D, center, displayRadius);
            Color color = GetColorForHeight(_pointHeights[i]);

            Raylib.DrawCircleV(screenPos, 2.0f, color);
        }
    }
}