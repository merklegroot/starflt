using Raylib_cs;
using StarflightGame;
using System;
using System.Collections.Generic;
using System.Numerics;
using StarflightGame.Views.StarMap;

namespace StarflightGame.Views;

public interface ICanopyStarSystemView
{
    void Update(float deltaTime, IStarMapView starMap);

    /// <param name="hoverHighlightRadiusPixels">If &gt; 0, the system within this distance of the crosshair (same as SPACE) gets a prominent name label.</param>
    void Draw(IShip ship, IStarMapView starMap, int viewWidth, int screenHeight, GameState currentState, Vector2 maneuverParallaxBoost, float hoverHighlightRadiusPixels);

    /// <summary>
    /// Screen position of a star's center, using the same math as <see cref="Draw"/> (rounding + wobble).
    /// </summary>
    void GetStarScreenPosition(
        IShip ship,
        StarSystem system,
        int viewWidth,
        int screenHeight,
        Vector2 maneuverParallaxBoost,
        out int screenX,
        out int screenY);

    /// <summary>
    /// The system whose drawn position is closest to the view center within <paramref name="maxDistancePixels"/> (same math as <see cref="Draw"/>).
    /// </summary>
    StarSystem? FindSystemNearCrosshair(
        IShip ship,
        IStarMapView starMap,
        int viewWidth,
        int screenHeight,
        Vector2 maneuverParallaxBoost,
        float maxDistancePixels);
}


/// <summary>
/// Main cockpit view: star systems from <see cref="StarMapView"/> drawn around the screen center with parallax,
/// glow, orbiting particles, and labels when close; draws the ship sprite at the center via <see cref="ShipRenderer"/>.
/// </summary>
public sealed class CanopyStarSystemView : ICanopyStarSystemView
{
    private const float WobbleAmount = 1.5f;

    private const float WobbleSpeed = 2.0f;

    private readonly Dictionary<StarSystem, List<StarParticle>> _particles = new Dictionary<StarSystem, List<StarParticle>>();
    private float _wobbleTime = 0.0f;

    public void Update(float deltaTime, IStarMapView starMap)
    {
        _wobbleTime += deltaTime;

        foreach (var system in starMap.GetAllSystems())
        {
            if (!_particles.ContainsKey(system))
            {
                _particles[system] = CreateParticlesForSystem(system);
            }

            foreach (var particle in _particles[system])
            {
                particle.WobblePhase += particle.WobbleSpeed * deltaTime;
                particle.Life += deltaTime;
                if (particle.Life > particle.MaxLife)
                {
                    particle.Life = 0.0f;
                    particle.WobblePhase = (float)(new Random().NextDouble() * Math.PI * 2);
                }
            }
        }
    }

    public void GetStarScreenPosition(
        IShip ship,
        StarSystem system,
        int viewWidth,
        int screenHeight,
        Vector2 maneuverParallaxBoost,
        out int screenX,
        out int screenY)
    {
        GetWobbleOffsets(out float wobbleX, out float wobbleY);
        ComputeStarScreenPosition(ship, system, viewWidth, screenHeight, maneuverParallaxBoost, wobbleX, wobbleY, out screenX, out screenY);
    }

    public StarSystem? FindSystemNearCrosshair(
        IShip ship,
        IStarMapView starMap,
        int viewWidth,
        int screenHeight,
        Vector2 maneuverParallaxBoost,
        float maxDistancePixels)
    {
        int shipCenterX = viewWidth / 2;
        int shipCenterY = screenHeight / 2;
        GetWobbleOffsets(out float wobbleX, out float wobbleY);

        StarSystem? best = null;
        float bestDistSq = float.MaxValue;

        foreach (var system in starMap.GetAllSystems())
        {
            ComputeStarScreenPosition(ship, system, viewWidth, screenHeight, maneuverParallaxBoost, wobbleX, wobbleY, out int sx, out int sy);
            float dx = sx - shipCenterX;
            float dy = sy - shipCenterY;
            float dSq = dx * dx + dy * dy;
            if (dSq < bestDistSq)
            {
                bestDistSq = dSq;
                best = system;
            }
        }

        float maxSq = maxDistancePixels * maxDistancePixels;
        if (best != null && bestDistSq <= maxSq)
        {
            return best;
        }

        return null;
    }

    private void GetWobbleOffsets(out float wobbleX, out float wobbleY)
    {
        wobbleX = MathF.Sin(_wobbleTime * WobbleSpeed) * WobbleAmount;
        wobbleY = MathF.Cos(_wobbleTime * WobbleSpeed * 1.3f) * WobbleAmount;
    }

    private static void ComputeStarScreenPosition(
        IShip ship,
        StarSystem system,
        int viewWidth,
        int screenHeight,
        Vector2 maneuverParallaxBoost,
        float wobbleX,
        float wobbleY,
        out int screenX,
        out int screenY)
    {
        int shipCenterX = viewWidth / 2;
        int shipCenterY = screenHeight / 2;
        Vector2 relativePos = system.Position - ship.Position + maneuverParallaxBoost;
        int baseScreenX = shipCenterX + (int)MathF.Round(relativePos.X);
        int baseScreenY = shipCenterY + (int)MathF.Round(relativePos.Y);
        screenX = baseScreenX + (int)wobbleX;
        screenY = baseScreenY + (int)wobbleY;
    }

    public void Draw(IShip ship, IStarMapView starMap, int viewWidth, int screenHeight, GameState currentState, Vector2 maneuverParallaxBoost, float hoverHighlightRadiusPixels)
    {
        int shipCenterX = viewWidth / 2;
        int shipCenterY = screenHeight / 2;
        GetWobbleOffsets(out float wobbleX, out float wobbleY);

        StarSystem? hoveredSystem = null;
        if (hoverHighlightRadiusPixels > 0f)
        {
            hoveredSystem = FindSystemNearCrosshair(ship, starMap, viewWidth, screenHeight, maneuverParallaxBoost, hoverHighlightRadiusPixels);
        }

        foreach (var system in starMap.GetAllSystems())
        {
            ComputeStarScreenPosition(ship, system, viewWidth, screenHeight, maneuverParallaxBoost, wobbleX, wobbleY, out int screenX, out int screenY);

            var particlesList = _particles[system];

            if (screenX > -50 && screenX < viewWidth + 50 && screenY > -50 && screenY < screenHeight + 50)
            {
                const int starRadius = 20;

                Color outerGlow = new Color(system.StarColor.R, system.StarColor.G, system.StarColor.B, (byte)(system.StarColor.A * 0.1f));
                Raylib.DrawCircle(screenX, screenY, starRadius + 8, outerGlow);

                Color midGlow = new Color(system.StarColor.R, system.StarColor.G, system.StarColor.B, (byte)(system.StarColor.A * 0.3f));
                Raylib.DrawCircle(screenX, screenY, starRadius + 4, midGlow);

                Color coreGlow = new Color(system.StarColor.R, system.StarColor.G, system.StarColor.B, (byte)(system.StarColor.A * 0.5f));
                Raylib.DrawCircle(screenX, screenY, starRadius + 2, coreGlow);

                Color translucentStarColor = new Color(system.StarColor.R, system.StarColor.G, system.StarColor.B, (byte)(system.StarColor.A * 0.7f));
                Raylib.DrawCircle(screenX, screenY, starRadius, translucentStarColor);

                Color brightCenter = new Color((byte)255, (byte)255, (byte)255, (byte)(system.StarColor.A * 0.6f));
                Raylib.DrawCircle(screenX, screenY, starRadius / 2, brightCenter);

                foreach (var particle in particlesList)
                {
                    float alpha = 1.0f - (particle.Life / particle.MaxLife);

                    float particleWobbleX = MathF.Sin(particle.WobblePhase) * particle.WobbleAmount;
                    float particleWobbleY = MathF.Cos(particle.WobblePhase * 1.3f) * particle.WobbleAmount;

                    float particleX = screenX + particle.BasePosition.X + particleWobbleX;
                    float particleY = screenY + particle.BasePosition.Y + particleWobbleY;

                    Color particleColor = new Color(
                        particle.Color.R,
                        particle.Color.G,
                        particle.Color.B,
                        (byte)(particle.Color.A * alpha * 0.8f));

                    Raylib.DrawCircle((int)particleX, (int)particleY, (int)particle.Size, particleColor);
                }

                const int nameFontSizeNormal = 16;
                const int nameFontSizeHovered = 24;
                const int nameGap = 8;
                bool isHovered = hoveredSystem != null && system.Id == hoveredSystem.Id;
                int nameFontSize = isHovered ? nameFontSizeHovered : nameFontSizeNormal;
                float labelAboveY = screenY - starRadius - nameFontSize - 12;
                float labelBelowY = screenY + starRadius + nameGap;

                // Prefer above the star; if that would clip off the top, place below instead.
                if (labelAboveY >= nameFontSize)
                {
                    if (isHovered)
                    {
                        Color fill = new Color(255, 250, 215, 255);
                        Color outline = new Color(18, 12, 42, 255);
                        UiText.DrawTextCenteredAtXOutlined(system.Name, screenX, labelAboveY, nameFontSize, fill, outline);
                    }
                    else
                    {
                        UiText.DrawTextCenteredAtX(system.Name, screenX, labelAboveY, nameFontSize, Color.WHITE);
                    }
                }
                else
                {
                    if (isHovered)
                    {
                        Color fill = new Color(255, 250, 215, 255);
                        Color outline = new Color(18, 12, 42, 255);
                        UiText.DrawTextCenteredAtXOutlined(system.Name, screenX, labelBelowY, nameFontSize, fill, outline);
                    }
                    else
                    {
                        UiText.DrawTextCenteredAtX(system.Name, screenX, labelBelowY, nameFontSize, Color.WHITE);
                    }
                }
            }
        }

        float rotation = ship.Rotation;
        bool forwardThrust = currentState == GameState.Maneuver && ship.ManeuverThrustForward;
        bool reverseThrust = currentState == GameState.Maneuver && ship.ManeuverThrustReverse;
        ShipRenderer.Draw(shipCenterX, shipCenterY, rotation, forwardThrust, reverseThrust);
    }

    private static List<StarParticle> CreateParticlesForSystem(StarSystem system)
    {
        var particles = new List<StarParticle>();
        var random = new Random(system.Name.GetHashCode());
        const int particleCount = 12;

        for (int i = 0; i < particleCount; i++)
        {
            float angle = (float)(random.NextDouble() * Math.PI * 2);
            float distance = 25 + (float)(random.NextDouble() * 10);
            Vector2 basePos = new Vector2(
                MathF.Cos(angle) * distance,
                MathF.Sin(angle) * distance);

            particles.Add(new StarParticle
            {
                BasePosition = basePos,
                WobblePhase = (float)(random.NextDouble() * Math.PI * 2),
                WobbleSpeed = 1.5f + (float)(random.NextDouble() * 1.0f),
                WobbleAmount = 2.0f + (float)(random.NextDouble() * 3.0f),
                Size = 1.5f + (float)(random.NextDouble() * 2.0f),
                Color = new Color(system.StarColor.R, system.StarColor.G, system.StarColor.B, (byte)(system.StarColor.A * 0.6f)),
                Life = (float)(random.NextDouble() * 5.0f),
                MaxLife = 5.0f
            });
        }

        return particles;
    }

    private sealed class StarParticle
    {
        public Vector2 BasePosition { get; set; }
        public float WobblePhase { get; set; }
        public float WobbleSpeed { get; set; }
        public float WobbleAmount { get; set; }
        public float Size { get; set; }
        public Color Color { get; set; }
        public float Life { get; set; }
        public float MaxLife { get; set; }
    }
}
