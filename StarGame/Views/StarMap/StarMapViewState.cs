using System.Numerics;

namespace StarflightGame.Views.StarMap;

/// <summary>
/// Camera pan and zoom for the star map view.
/// </summary>
public sealed record StarMapViewState(
    Vector2 CameraOffset = default,
    float Zoom = 1.0f)
{
    public const float MinZoom = 0.5f;
    public const float MaxZoom = 3.0f;
}
