using Raylib_cs;
using System;
using System.Numerics;

namespace StarflightGame;

/// <summary>Manages render texture, rotation, and drawing for the procedural planet views.</summary>
public sealed class PlanetViewRenderer
{
    private readonly Random _regenRandom = new Random();
    private RenderTexture2D? _renderTexture = null;
    private float _rotationAngle = 0.0f;

    public void ResetRotation()
    {
        _rotationAngle = 0.0f;
    }

    public void Unload()
    {
        if (_renderTexture != null)
        {
            Raylib.UnloadRenderTexture(_renderTexture.Value);
            _renderTexture = null;
        }
    }

    public string CreateUniquePlanetName(string systemName)
    {
        return systemName + " I " + _regenRandom.Next(1000000) + "_" + DateTime.Now.Ticks + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
    }

    public void DrawExplorationPanel(Planet planet, int panelX, int panelY, int panelWidth, int panelHeight)
    {
        EnsureTexture(panelWidth, panelHeight);

        float displayRadius = Math.Min(panelWidth, panelHeight) * 0.3f;
        AdvanceRotation();

        planet.DrawSpherePointsToTexture(_renderTexture!.Value, displayRadius, _rotationAngle);

        Raylib.DrawTextureRec(
            _renderTexture.Value.Texture,
            new Rectangle(0, 0, panelWidth, -panelHeight),
            new Vector2(panelX, panelY),
            Color.WHITE);

        Raylib.DrawRectangleLines(panelX, panelY, panelWidth, panelHeight, Color.GRAY);
    }

    public void DrawEncounterFullBleed(Planet planet, int viewWidth, int viewHeight)
    {
        EnsureTexture(viewWidth, viewHeight);

        float displayRadius = Math.Min(viewWidth, viewHeight) * 0.3f;
        AdvanceRotation();

        planet.DrawSpherePointsToTexture(_renderTexture!.Value, displayRadius, _rotationAngle);

        Raylib.DrawTextureRec(
            _renderTexture.Value.Texture,
            new Rectangle(0, 0, viewWidth, -viewHeight),
            Vector2.Zero,
            Color.WHITE);
    }

    private void EnsureTexture(int width, int height)
    {
        if (_renderTexture == null || _renderTexture.Value.Texture.Width != width || _renderTexture.Value.Texture.Height != height)
        {
            if (_renderTexture != null)
            {
                Raylib.UnloadRenderTexture(_renderTexture.Value);
            }

            _renderTexture = Raylib.LoadRenderTexture(width, height);
        }
    }

    private void AdvanceRotation()
    {
        float deltaTime = Raylib.GetFrameTime();
        const float rotationSpeed = 0.5f;
        _rotationAngle += rotationSpeed * deltaTime;
        if (_rotationAngle >= MathF.PI * 2.0f)
        {
            _rotationAngle -= MathF.PI * 2.0f;
        }
    }
}
