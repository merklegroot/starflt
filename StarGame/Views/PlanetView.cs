using Raylib_cs;
using StarflightGame.Mining;
using System.Numerics;

namespace StarflightGame.Views;

public interface IPlanetView
{
    float EncounterRotationAngle { get; }

    void ResetRotation();

    void Unload();

    string CreateUniquePlanetName(string systemName);

    void DrawExplorationPanel(Planet planet, int panelX, int panelY, int panelWidth, int panelHeight);

    void DrawEncounterFullBleed(Planet planet, int viewWidth, int viewHeight);

    void DrawMiningRigMarkers(Planet planet, int viewWidth, int viewHeight, IReadOnlyList<MiningRig> rigs);
}


/// <summary>
/// Draws a rotating planet preview into a render texture for the exploration panel or full-screen encounter.
/// Manages texture lifetime, sphere rendering via <see cref="Planet"/>, and unique generated planet names.
/// </summary>
public sealed class PlanetView : IPlanetView
{
    private readonly Random _regenRandom = new Random();
    private RenderTexture2D? _renderTexture = null;
    private float _rotationAngle = 0.0f;

    public float EncounterRotationAngle => _rotationAngle;

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

    public void DrawMiningRigMarkers(Planet planet, int viewWidth, int viewHeight, IReadOnlyList<MiningRig> rigs)
    {
        if (rigs.Count == 0)
        {
            return;
        }

        float rotationAngle = _rotationAngle;
        for (int i = 0; i < rigs.Count; i++)
        {
            if (!PlanetEncounterRender.TryProjectSurfaceDirection(
                    rigs[i].SurfaceDirection,
                    viewWidth,
                    viewHeight,
                    planet,
                    rotationAngle,
                    out Vector2 screen))
            {
                continue;
            }

            const int outerRadius = 7;
            const int innerRadius = 3;
            Raylib.DrawCircleV(screen, outerRadius, new Color(255, 180, 40, 220));
            Raylib.DrawCircleV(screen, innerRadius, new Color(40, 28, 8, 255));
            Raylib.DrawCircleLines((int)screen.X, (int)screen.Y, outerRadius, new Color(255, 230, 120, 255));
        }
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
