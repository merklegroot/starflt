using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace StarflightGame;

internal sealed class CanopyStarSystemView
{
    private readonly Dictionary<StarSystem, List<StarParticle>> _particles = new Dictionary<StarSystem, List<StarParticle>>();
    private float _wobbleTime = 0.0f;

    public void Update(float deltaTime, StarMapView starMap)
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

    public void Draw(Ship ship, StarMapView starMap, int viewWidth, int screenHeight, GameState currentState)
    {
        int shipCenterX = viewWidth / 2;
        int shipCenterY = screenHeight / 2;

        foreach (var system in starMap.GetAllSystems())
        {
            Vector2 relativePos = system.Position - ship.Position;

            const float wobbleAmount = 1.5f;
            const float wobbleSpeed = 2.0f;
            float wobbleX = MathF.Sin(_wobbleTime * wobbleSpeed) * wobbleAmount;
            float wobbleY = MathF.Cos(_wobbleTime * wobbleSpeed * 1.3f) * wobbleAmount;

            int baseScreenX = shipCenterX + (int)relativePos.X;
            int baseScreenY = shipCenterY + (int)relativePos.Y;
            int screenX = baseScreenX + (int)wobbleX;
            int screenY = baseScreenY + (int)wobbleY;

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

                float distance = Vector2.Distance(ship.Position, system.Position);
                if (distance < 300)
                {
                    int nameY = screenY - starRadius - 25;
                    if (nameY > 100)
                    {
                        Raylib.DrawText(system.Name, screenX - Raylib.MeasureText(system.Name, 16) / 2, nameY, 16, Color.WHITE);
                    }
                }
            }
        }

        float rotation = currentState == GameState.Maneuver ? ship.Rotation : -MathF.PI / 2.0f;
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
