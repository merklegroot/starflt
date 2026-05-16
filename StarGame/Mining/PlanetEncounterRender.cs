using System.Numerics;
using Raylib_cs;

namespace StarflightGame.Mining;

/// <summary>
/// Camera and scale math shared with <see cref="Planet.DrawSpherePointsToTexture"/> for surface picking and rig markers.
/// </summary>
public static class PlanetEncounterRender
{
    public static float GetDisplayRadius(int viewWidth, int viewHeight) =>
        Math.Min(viewWidth, viewHeight) * 0.3f;

    public static float GetSphereWorldRadius(Planet planet, float displayRadius)
    {
        float ringOuter = GetRingOuterRadiusDisplay(planet, displayRadius);
        float maxExtent = MathF.Max(displayRadius, ringOuter);
        if (maxExtent < 1e-6f)
        {
            maxExtent = displayRadius;
        }

        float worldScale = displayRadius / maxExtent;
        return displayRadius * worldScale;
    }

    public static Camera3D BuildCamera(float displayRadius)
    {
        float cameraDistance = displayRadius * 2.5f;
        return new Camera3D
        {
            Position = new Vector3(0, 0, cameraDistance),
            Target = Vector3.Zero,
            Up = new Vector3(0, 1, 0),
            FovY = 60.0f,
            Projection = CameraProjection.CAMERA_PERSPECTIVE
        };
    }

    public static Vector3 RotateAroundY(Vector3 v, float rotationAngle)
    {
        float c = MathF.Cos(rotationAngle);
        float s = MathF.Sin(rotationAngle);
        return new Vector3(
            v.X * c - v.Z * s,
            v.Y,
            v.X * s + v.Z * c);
    }

    public static Vector3 RotateAroundYInverse(Vector3 v, float rotationAngle) =>
        RotateAroundY(v, -rotationAngle);

    public static bool TryPickSurfaceDirection(
        Vector2 screenPosition,
        int viewWidth,
        int viewHeight,
        Planet planet,
        float rotationAngle,
        out Vector3 surfaceDirection)
    {
        surfaceDirection = Vector3.Zero;
        float displayRadius = GetDisplayRadius(viewWidth, viewHeight);
        float sphereRadius = GetSphereWorldRadius(planet, displayRadius);
        Camera3D camera = BuildCamera(displayRadius);

        Ray ray = BuildPickRay(screenPosition, viewWidth, viewHeight, camera);
        if (!TryRaySphereIntersection(ray, Vector3.Zero, sphereRadius, out Vector3 hitWorld))
        {
            return false;
        }

        surfaceDirection = RotateAroundYInverse(hitWorld / sphereRadius, rotationAngle);
        float lenSq = surfaceDirection.LengthSquared();
        if (lenSq < 1e-6f)
        {
            return false;
        }

        surfaceDirection = Vector3.Normalize(surfaceDirection);
        return true;
    }

    public static bool TryProjectSurfaceDirection(
        Vector3 surfaceDirection,
        int viewWidth,
        int viewHeight,
        Planet planet,
        float rotationAngle,
        out Vector2 screenPosition)
    {
        screenPosition = Vector2.Zero;
        float displayRadius = GetDisplayRadius(viewWidth, viewHeight);
        float sphereRadius = GetSphereWorldRadius(planet, displayRadius);
        Camera3D camera = BuildCamera(displayRadius);

        Vector3 world = RotateAroundY(surfaceDirection, rotationAngle) * sphereRadius;
        Vector2 projected = Raylib.GetWorldToScreenEx(world, camera, viewWidth, viewHeight);
        if (projected.X < -5000f || projected.Y < -5000f)
        {
            return false;
        }

        screenPosition = projected;
        return true;
    }

    private static Ray BuildPickRay(Vector2 screenPosition, int viewWidth, int viewHeight, Camera3D camera)
    {
        float ndcX = (2.0f * screenPosition.X / viewWidth) - 1.0f;
        float ndcY = 1.0f - (2.0f * screenPosition.Y / viewHeight);
        float aspect = viewWidth / (float)viewHeight;

        Vector3 forward = Vector3.Normalize(camera.Target - camera.Position);
        Vector3 right = Vector3.Normalize(Vector3.Cross(forward, camera.Up));
        Vector3 up = Vector3.Normalize(Vector3.Cross(right, forward));
        float tanHalfFov = MathF.Tan(camera.FovY * MathF.PI / 180f * 0.5f);

        Vector3 direction = Vector3.Normalize(
            forward + right * (ndcX * tanHalfFov * aspect) + up * (ndcY * tanHalfFov));

        return new Ray
        {
            Position = camera.Position,
            Direction = direction
        };
    }

    private static bool TryRaySphereIntersection(Ray ray, Vector3 center, float radius, out Vector3 hitPoint)
    {
        hitPoint = Vector3.Zero;
        Vector3 oc = ray.Position - center;
        float b = Vector3.Dot(oc, ray.Direction);
        float c = Vector3.Dot(oc, oc) - radius * radius;
        float discriminant = b * b - c;
        if (discriminant < 0f)
        {
            return false;
        }

        float sqrtD = MathF.Sqrt(discriminant);
        float t0 = -b - sqrtD;
        float t1 = -b + sqrtD;
        float t = t0 >= 0f ? t0 : t1;
        if (t < 0f)
        {
            return false;
        }

        hitPoint = ray.Position + ray.Direction * t;
        return true;
    }

    private static float GetRingOuterRadiusDisplay(Planet planet, float displayRadius)
    {
        if (!planet.Rings.HasValue || !planet.Rings.Value.IsValid)
        {
            return 0f;
        }

        PlanetRingData ring = planet.Rings.Value;

        if (planet.RadiusKm > 0f)
        {
            float kmToDisplay = displayRadius / planet.RadiusKm;
            return ring.OuterRadiusKm * kmToDisplay;
        }

        float kmToDisplayFallback = displayRadius / MathF.Max(ring.OuterRadiusKm, 1f);
        return ring.OuterRadiusKm * kmToDisplayFallback;
    }
}
