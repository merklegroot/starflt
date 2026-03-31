using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Numerics;
using StarflightGame.Constants;

namespace StarflightGame;

public interface IParallaxStarfield
{
    void Generate(int screenWidth, int screenHeight);

    void UpdateTwinkling(float deltaTime);

    void ApplyMovement(Vector2 movement, int screenWidth, int screenHeight, float deltaTime);

    void Draw(int screenWidth, int screenHeight);
}


public sealed class ParallaxStarfield : IParallaxStarfield
{
    private readonly List<StarLayer> _layers = new List<StarLayer>();
    private readonly Random _random = new Random();

    public void Generate(int screenWidth, int screenHeight)
    {
        _layers.Clear();
        int viewWidth = LayoutConstants.MainViewWidth(screenWidth);

        var veryFarLayer = new StarLayer
        {
            SpeedMultiplier = 4.0f,
            BaseColor = new Color(100, 100, 120, 255),
            MinBrightness = 0.2f,
            MaxBrightness = 0.5f,
            EnableTwinkle = true
        };
        for (int i = 0; i < 300; i++)
        {
            veryFarLayer.Stars.Add(new Star
            {
                Position = new Vector2(
                    _random.Next(0, viewWidth),
                    _random.Next(0, screenHeight)),
                Brightness = (float)(_random.NextDouble() * 0.3 + 0.2),
                TwinklePhase = (float)(_random.NextDouble() * Math.PI * 2),
                TwinkleSpeed = (float)(_random.NextDouble() * 0.5 + 0.3),
                BaseColor = new Color(100, 100, 120, 255),
                Size = 0.5f
            });
        }
        _layers.Add(veryFarLayer);

        var farLayer = new StarLayer
        {
            SpeedMultiplier = 8.0f,
            BaseColor = new Color(150, 150, 160, 255),
            MinBrightness = 0.4f,
            MaxBrightness = 0.7f,
            EnableTwinkle = true
        };
        for (int i = 0; i < 200; i++)
        {
            farLayer.Stars.Add(new Star
            {
                Position = new Vector2(
                    _random.Next(0, viewWidth),
                    _random.Next(0, screenHeight)),
                Brightness = (float)(_random.NextDouble() * 0.3 + 0.4),
                TwinklePhase = (float)(_random.NextDouble() * Math.PI * 2),
                TwinkleSpeed = (float)(_random.NextDouble() * 0.8 + 0.5),
                BaseColor = new Color(150, 150, 160, 255),
                Size = 0.8f
            });
        }
        _layers.Add(farLayer);

        var midLayer = new StarLayer
        {
            SpeedMultiplier = 20.0f,
            BaseColor = Color.WHITE,
            MinBrightness = 0.6f,
            MaxBrightness = 1.0f,
            EnableTwinkle = true
        };
        for (int i = 0; i < 150; i++)
        {
            Color starColor = Color.WHITE;
            float colorChoice = (float)_random.NextDouble();
            if (colorChoice < 0.3f)
                starColor = new Color(200, 220, 255, 255);
            else if (colorChoice < 0.6f)
                starColor = new Color(255, 250, 200, 255);
            else
                starColor = Color.WHITE;

            midLayer.Stars.Add(new Star
            {
                Position = new Vector2(
                    _random.Next(0, viewWidth),
                    _random.Next(0, screenHeight)),
                Brightness = (float)(_random.NextDouble() * 0.4 + 0.6),
                TwinklePhase = (float)(_random.NextDouble() * Math.PI * 2),
                TwinkleSpeed = (float)(_random.NextDouble() * 1.0 + 0.7),
                BaseColor = starColor,
                Size = 1.0f
            });
        }
        _layers.Add(midLayer);

        var closeLayer = new StarLayer
        {
            SpeedMultiplier = 48.0f,
            BaseColor = Color.WHITE,
            MinBrightness = 0.8f,
            MaxBrightness = 1.0f,
            EnableTwinkle = false
        };
        for (int i = 0; i < 80; i++)
        {
            Color starColor = Color.WHITE;
            float colorChoice = (float)_random.NextDouble();
            if (colorChoice < 0.25f)
                starColor = new Color(180, 200, 255, 255);
            else if (colorChoice < 0.5f)
                starColor = new Color(255, 240, 180, 255);
            else if (colorChoice < 0.7f)
                starColor = new Color(255, 200, 200, 255);
            else
                starColor = Color.WHITE;

            closeLayer.Stars.Add(new Star
            {
                Position = new Vector2(
                    _random.Next(0, viewWidth),
                    _random.Next(0, screenHeight)),
                Brightness = (float)(_random.NextDouble() * 0.2 + 0.8),
                TwinklePhase = 0.0f,
                TwinkleSpeed = 0.0f,
                BaseColor = starColor,
                Size = (float)(_random.NextDouble() * 1.5 + 1.5)
            });
        }
        _layers.Add(closeLayer);

        var veryCloseLayer = new StarLayer
        {
            SpeedMultiplier = 80.0f,
            BaseColor = Color.WHITE,
            MinBrightness = 1.0f,
            MaxBrightness = 1.0f,
            EnableTwinkle = false
        };
        for (int i = 0; i < 20; i++)
        {
            Color starColor = Color.WHITE;
            float colorChoice = (float)_random.NextDouble();
            if (colorChoice < 0.3f)
                starColor = new Color(150, 180, 255, 255);
            else if (colorChoice < 0.6f)
                starColor = new Color(255, 220, 150, 255);
            else
                starColor = Color.WHITE;

            veryCloseLayer.Stars.Add(new Star
            {
                Position = new Vector2(
                    _random.Next(0, viewWidth),
                    _random.Next(0, screenHeight)),
                Brightness = 1.0f,
                TwinklePhase = 0.0f,
                TwinkleSpeed = 0.0f,
                BaseColor = starColor,
                Size = (float)(_random.NextDouble() * 2.0 + 3.0)
            });
        }
        _layers.Add(veryCloseLayer);
    }

    public void UpdateTwinkling(float deltaTime)
    {
        foreach (var layer in _layers)
        {
            if (!layer.EnableTwinkle) continue;

            for (int i = 0; i < layer.Stars.Count; i++)
            {
                Star star = layer.Stars[i];
                if (star.TwinkleSpeed > 0)
                {
                    star.TwinklePhase += star.TwinkleSpeed * deltaTime;
                    float twinkle = (MathF.Sin(star.TwinklePhase) + 1.0f) * 0.5f;
                    star.Brightness = layer.MinBrightness + (layer.MaxBrightness - layer.MinBrightness) * twinkle;
                    layer.Stars[i] = star;
                }
            }
        }
    }

    public void ApplyMovement(Vector2 movement, int screenWidth, int screenHeight, float deltaTime)
    {
        int viewWidth = LayoutConstants.MainViewWidth(screenWidth);

        foreach (var layer in _layers)
        {
            Vector2 layerMovement = movement * layer.SpeedMultiplier;

            for (int i = 0; i < layer.Stars.Count; i++)
            {
                Star star = layer.Stars[i];

                star.Position += layerMovement;

                if (layer.EnableTwinkle && star.TwinkleSpeed > 0)
                {
                    star.TwinklePhase += star.TwinkleSpeed * deltaTime;
                    float twinkle = (MathF.Sin(star.TwinklePhase) + 1.0f) * 0.5f;
                    star.Brightness = layer.MinBrightness + (layer.MaxBrightness - layer.MinBrightness) * twinkle;
                }

                Vector2 pos = star.Position;
                if (pos.X < 0)
                    pos.X = viewWidth;
                else if (pos.X > viewWidth)
                    pos.X = 0;

                if (pos.Y < 0)
                    pos.Y = screenHeight;
                else if (pos.Y > screenHeight)
                    pos.Y = 0;

                star.Position = pos;

                layer.Stars[i] = star;
            }
        }
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        foreach (var layer in _layers)
        {
            foreach (var star in layer.Stars)
            {
                Color finalColor = new Color(
                    (byte)(star.BaseColor.R * star.Brightness),
                    (byte)(star.BaseColor.G * star.Brightness),
                    (byte)(star.BaseColor.B * star.Brightness),
                    star.BaseColor.A);

                float size = star.Size;

                if (size < 1.0f)
                {
                    Raylib.DrawPixel((int)star.Position.X, (int)star.Position.Y, finalColor);
                }
                else if (size < 2.0f)
                {
                    Raylib.DrawCircle((int)star.Position.X, (int)star.Position.Y, 1, finalColor);
                }
                else if (size < 3.5f)
                {
                    Color glowColor = new Color(finalColor.R, finalColor.G, finalColor.B, (byte)(finalColor.A * 0.3f));
                    Raylib.DrawCircle((int)star.Position.X, (int)star.Position.Y, (int)size + 1, glowColor);
                    Raylib.DrawCircle((int)star.Position.X, (int)star.Position.Y, (int)size, finalColor);
                }
                else
                {
                    Color outerGlow = new Color(finalColor.R, finalColor.G, finalColor.B, (byte)(finalColor.A * 0.2f));
                    Raylib.DrawCircle((int)star.Position.X, (int)star.Position.Y, (int)size + 2, outerGlow);
                    Color midGlow = new Color(finalColor.R, finalColor.G, finalColor.B, (byte)(finalColor.A * 0.5f));
                    Raylib.DrawCircle((int)star.Position.X, (int)star.Position.Y, (int)size + 1, midGlow);
                    Raylib.DrawCircle((int)star.Position.X, (int)star.Position.Y, (int)size, finalColor);
                    Color brightCenter = new Color((byte)255, (byte)255, (byte)255, (byte)(finalColor.A * 0.8f));
                    Raylib.DrawCircle((int)star.Position.X, (int)star.Position.Y, (int)(size * 0.5f), brightCenter);
                }
            }
        }
    }

    private sealed class Star
    {
        public Vector2 Position { get; set; }
        public float Brightness { get; set; } = 1.0f;
        public float TwinklePhase { get; set; } = 0.0f;
        public float TwinkleSpeed { get; set; } = 0.0f;
        public Color BaseColor { get; set; } = Color.WHITE;
        public float Size { get; set; } = 1.0f;
    }

    private sealed class StarLayer
    {
        public List<Star> Stars { get; set; } = new List<Star>();
        public float SpeedMultiplier { get; set; } = 1.0f;
        public Color BaseColor { get; set; } = Color.WHITE;
        public float MinBrightness { get; set; } = 0.3f;
        public float MaxBrightness { get; set; } = 1.0f;
        public bool EnableTwinkle { get; set; } = false;
    }
}
