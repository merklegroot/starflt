using Raylib_cs;
using System.Numerics;
using StarflightGame.Views;

namespace StarflightGame.Combat;

public interface ICombatView
{
    bool IsVictory { get; }
    bool IsDefeat { get; }
    Vector2 PlayerPosition { get; }

    void BeginSession(int viewWidth, int viewHeight, IShip ship);

    void Update(float deltaTime, int viewWidth, int viewHeight, IShip ship);

    void Draw(int viewWidth, int viewHeight, IShip ship);

    void RestoreForDebug();
}

public sealed class CombatView : ICombatView
{
    private const float TurnSpeed = 4.5f;
    private const float ThrustAcceleration = 220f;
    private const float ReverseThrustMultiplier = 0.5f;
    private const float DragPerSecond = 0.5f;
    private const float VelocityStopEpsilonSq = 0.01f;
    private const float MaxSpeed = 280f;
    private const float ArenaRadiusFraction = 0.42f;
    private const float PlayerPhaserCooldown = 0.22f;
    private const float PlayerPhaserSpeed = 520f;
    private const float PlayerPhaserDamage = 12f;
    private const float ProjectileLifetime = 2.5f;
    private const float EnemyMaxSpeed = 160f;
    private const float EnemyThrust = 140f;
    private const float EnemyTurnSpeed = 2.8f;
    private const float EnemyHullMax = 36f;
    private const float EnemyFireCooldown = 1.4f;
    private const float EnemyShotSpeed = 300f;
    private const float EnemyShotDamage = 16f;
    private const float ExplosionFlashDuration = 0.35f;
    private const int PlayerExplosionParticleCount = 40;
    private const int EnemyExplosionParticleCount = 28;
    private const float EnemyFireRange = 340f;
    private const float EnemyFireAngleTolerance = 0.35f;
    private const int VictoryCredits = 500;
    private const float ShieldRegenPerSecond = 4f;
    private const int HealthBarWidth = 34;
    private const int HealthBarHeight = 4;
    private const int HealthBarGap = 2;

    private readonly List<CombatEnemy> _enemies = new List<CombatEnemy>();
    private readonly List<CombatProjectile> _projectiles = new List<CombatProjectile>();
    private readonly List<CombatExplosionBurst> _explosionBursts = new List<CombatExplosionBurst>();
    private Vector2 _playerPosition = Vector2.Zero;
    private Vector2 _playerVelocity = Vector2.Zero;
    private float _playerFireCooldown;
    private float _arenaRadius;
    private bool _sessionActive;
    private bool _outcomeHandled;
    public bool IsVictory { get; private set; }
    public bool IsDefeat { get; private set; }
    public Vector2 PlayerPosition => _playerPosition;

    public void RestoreForDebug()
    {
        IsDefeat = false;
        IsVictory = false;
        _outcomeHandled = false;
        _explosionBursts.Clear();

        bool anyAlive = false;
        for (int i = 0; i < _enemies.Count; i++)
        {
            if (_enemies[i].IsAlive)
            {
                anyAlive = true;
                break;
            }
        }

        if (!anyAlive)
        {
            SpawnEnemies();
        }
    }

    public void BeginSession(int viewWidth, int viewHeight, IShip ship)
    {
        _sessionActive = true;
        _outcomeHandled = false;
        IsVictory = false;
        IsDefeat = false;
        _playerPosition = Vector2.Zero;
        _playerVelocity = Vector2.Zero;
        _playerFireCooldown = 0f;
        _projectiles.Clear();
        _enemies.Clear();
        _explosionBursts.Clear();

        ship.Rotation = 0f;
        ship.Velocity = Vector2.Zero;
        ship.ManeuverThrustForward = false;
        ship.ManeuverThrustReverse = false;

        _arenaRadius = MathF.Min(viewWidth, viewHeight) * ArenaRadiusFraction;
        SpawnEnemies();
    }

    public void Update(float deltaTime, int viewWidth, int viewHeight, IShip ship)
    {
        if (!_sessionActive || IsVictory)
        {
            return;
        }

        if (IsDefeat)
        {
            UpdateExplosions(deltaTime);
            return;
        }

        _arenaRadius = MathF.Min(viewWidth, viewHeight) * ArenaRadiusFraction;

        UpdateShieldRegen(deltaTime, ship);
        UpdatePlayerMovement(deltaTime, ship);
        UpdatePlayerWeapons(deltaTime, viewWidth, ship);
        UpdateEnemies(deltaTime, ship);
        UpdateProjectiles(deltaTime, ship);
        UpdateExplosions(deltaTime);
        CheckOutcomes(ship);

        ship.Velocity = _playerVelocity;
    }

    public void Draw(int viewWidth, int viewHeight, IShip ship)
    {
        EncounterStarfield.Draw(viewWidth, viewHeight);

        int centerX = viewWidth / 2;
        int centerY = viewHeight / 2;

        DrawArenaBoundary(centerX, centerY, _arenaRadius);
        DrawProjectiles(centerX, centerY);
        DrawEnemies(centerX, centerY);
        DrawExplosions(centerX, centerY);
        Vector2 screenPos = new Vector2(centerX, centerY) + _playerPosition;
        if (!IsDefeat)
        {
            ShipRenderer.Draw(
                (int)screenPos.X,
                (int)screenPos.Y,
                ship.Rotation,
                ship.ManeuverThrustForward,
                ship.ManeuverThrustReverse);

            DrawPlayerHealthBars((int)screenPos.X, (int)screenPos.Y, ship);
        }

        DrawCombatHud(viewWidth, viewHeight, ship);
    }

    private void SpawnEnemies()
    {
        const int enemyCount = 3;
        for (int i = 0; i < enemyCount; i++)
        {
            float angle = (MathF.PI * 2f / enemyCount) * i + 0.4f;
            float dist = _arenaRadius * 0.82f;
            Vector2 offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * dist;

            _enemies.Add(new CombatEnemy
            {
                Position = offset,
                Velocity = Vector2.Zero,
                Rotation = angle + MathF.PI,
                Hull = EnemyHullMax,
                FireCooldown = 0.5f + i * 0.3f
            });
        }
    }

    private static void UpdateShieldRegen(float deltaTime, IShip ship)
    {
        if (ship.ShieldStrength < ship.MaxShieldStrength)
        {
            ship.RegenerateShields(ShieldRegenPerSecond * deltaTime);
        }
    }

    private void UpdatePlayerMovement(float deltaTime, IShip ship)
    {
        ship.ManeuverThrustForward = false;
        ship.ManeuverThrustReverse = false;

        float turnInput = 0f;
        if (Raylib.IsKeyDown(KeyboardKey.KEY_A))
            turnInput -= 1f;
        if (Raylib.IsKeyDown(KeyboardKey.KEY_D))
            turnInput += 1f;

        ship.Rotation += turnInput * TurnSpeed * deltaTime;

        bool wantForward = Raylib.IsKeyDown(KeyboardKey.KEY_W);
        bool wantReverse = Raylib.IsKeyDown(KeyboardKey.KEY_S);
        float thrustSign = 0f;
        if (wantForward && !wantReverse)
            thrustSign = 1f;
        else if (wantReverse && !wantForward)
            thrustSign = -1f;

        Vector2 forward = GetForward(ship.Rotation);

        if (thrustSign != 0f)
        {
            float accel = ThrustAcceleration * (thrustSign > 0f ? 1f : ReverseThrustMultiplier);
            _playerVelocity += forward * (accel * thrustSign * deltaTime);
            if (thrustSign > 0f)
                ship.ManeuverThrustForward = true;
            else
                ship.ManeuverThrustReverse = true;
        }
        else
        {
            float dragFactor = MathF.Exp(-DragPerSecond * deltaTime);
            _playerVelocity *= dragFactor;
            if (_playerVelocity.LengthSquared() < VelocityStopEpsilonSq)
                _playerVelocity = Vector2.Zero;
        }

        float speedSq = _playerVelocity.LengthSquared();
        if (speedSq > MaxSpeed * MaxSpeed)
            _playerVelocity = Vector2.Normalize(_playerVelocity) * MaxSpeed;

        _playerPosition += _playerVelocity * deltaTime;
        ClampToArena(ref _playerPosition);
    }

    private void UpdatePlayerWeapons(float deltaTime, int viewWidth, IShip ship)
    {
        _playerFireCooldown = MathF.Max(0f, _playerFireCooldown - deltaTime);

        Vector2 mouse = Raylib.GetMousePosition();
        bool mouseInView = mouse.X >= 0 && mouse.X < viewWidth;
        bool fire = Raylib.IsKeyDown(KeyboardKey.KEY_SPACE)
            || (mouseInView && Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT));

        if (!fire || _playerFireCooldown > 0f)
        {
            return;
        }

        _playerFireCooldown = PlayerPhaserCooldown;
        Vector2 forward = GetForward(ship.Rotation);
        Vector2 muzzle = _playerPosition + forward * 28f;

        _projectiles.Add(new CombatProjectile
        {
            Position = muzzle,
            Velocity = forward * PlayerPhaserSpeed,
            IsPlayerOwned = true,
            Lifetime = ProjectileLifetime,
            Damage = PlayerPhaserDamage
        });
    }

    private void UpdateEnemies(float deltaTime, IShip ship)
    {
        for (int i = 0; i < _enemies.Count; i++)
        {
            CombatEnemy enemy = _enemies[i];
            if (!enemy.IsAlive)
            {
                continue;
            }

            Vector2 toPlayer = _playerPosition - enemy.Position;
            float dist = toPlayer.Length();
            if (dist > 0.01f)
            {
                float desiredAngle = MathF.Atan2(toPlayer.X, -toPlayer.Y);
                enemy.Rotation = RotateToward(enemy.Rotation, desiredAngle, EnemyTurnSpeed * deltaTime);
            }

            Vector2 forward = GetForward(enemy.Rotation);
            if (dist > 80f)
            {
                enemy.Velocity += forward * (EnemyThrust * deltaTime);
            }
            else
            {
                enemy.Velocity *= MathF.Exp(-DragPerSecond * deltaTime);
            }

            float enemySpeedSq = enemy.Velocity.LengthSquared();
            if (enemySpeedSq > EnemyMaxSpeed * EnemyMaxSpeed)
                enemy.Velocity = Vector2.Normalize(enemy.Velocity) * EnemyMaxSpeed;

            enemy.Position += enemy.Velocity * deltaTime;
            ClampToArena(ref enemy.Position);

            enemy.FireCooldown -= deltaTime;
            if (enemy.FireCooldown <= 0f && dist < EnemyFireRange && dist > 40f)
            {
                Vector2 aimDir = Vector2.Normalize(toPlayer);
                Vector2 fireDir = GetForward(enemy.Rotation);
                float dot = Vector2.Dot(aimDir, fireDir);
                if (dot > 1f - EnemyFireAngleTolerance)
                {
                    enemy.FireCooldown = EnemyFireCooldown;
                    Vector2 muzzle = enemy.Position + fireDir * 22f;
                    _projectiles.Add(new CombatProjectile
                    {
                        Position = muzzle,
                        Velocity = fireDir * EnemyShotSpeed,
                        IsPlayerOwned = false,
                        Lifetime = ProjectileLifetime,
                        Damage = EnemyShotDamage
                    });
                }
            }
        }
    }

    private void UpdateProjectiles(float deltaTime, IShip ship)
    {
        for (int i = _projectiles.Count - 1; i >= 0; i--)
        {
            CombatProjectile proj = _projectiles[i];
            proj.Position += proj.Velocity * deltaTime;
            proj.Lifetime -= deltaTime;

            if (proj.Lifetime <= 0f || proj.Position.Length() > _arenaRadius + 40f)
            {
                _projectiles.RemoveAt(i);
                continue;
            }

            if (proj.IsPlayerOwned)
            {
                bool hit = false;
                for (int e = 0; e < _enemies.Count; e++)
                {
                    CombatEnemy enemy = _enemies[e];
                    if (!enemy.IsAlive)
                    {
                        continue;
                    }

                    if (Vector2.Distance(proj.Position, enemy.Position) < 22f)
                    {
                        float hullBefore = enemy.Hull;
                        enemy.Hull -= proj.Damage;
                        if (hullBefore > 0f && enemy.Hull <= 0f)
                        {
                            SpawnExplosionAt(enemy.Position, EnemyExplosionParticleCount, 9000 + e);
                        }

                        hit = true;
                        break;
                    }
                }

                if (hit)
                {
                    _projectiles.RemoveAt(i);
                }
            }
            else if (Vector2.Distance(proj.Position, _playerPosition) < 20f)
            {
                ship.ApplyCombatDamage(proj.Damage);
                _projectiles.RemoveAt(i);
            }
        }
    }

    private void CheckOutcomes(IShip ship)
    {
        if (_outcomeHandled)
        {
            return;
        }

        if (ship.IsCombatDestroyed())
        {
            IsDefeat = true;
            _outcomeHandled = true;
            SpawnPlayerExplosion();
            return;
        }

        bool anyAlive = false;
        for (int i = 0; i < _enemies.Count; i++)
        {
            if (_enemies[i].IsAlive)
            {
                anyAlive = true;
                break;
            }
        }

        if (!anyAlive)
        {
            IsVictory = true;
            ship.AddCredits(VictoryCredits);
            _outcomeHandled = true;
        }
    }

    private void SpawnPlayerExplosion()
    {
        _playerVelocity = Vector2.Zero;
        SpawnExplosionAt(_playerPosition, PlayerExplosionParticleCount, 7711);
    }

    private void SpawnExplosionAt(Vector2 arenaPosition, int particleCount, int randomSeed)
    {
        CombatExplosionBurst burst = new CombatExplosionBurst
        {
            Position = arenaPosition,
            FlashTimer = ExplosionFlashDuration
        };

        Random rng = new Random(randomSeed);
        Color[] palette =
        {
            new Color(255, 240, 180, 255),
            new Color(255, 180, 60, 255),
            new Color(255, 110, 50, 255),
            new Color(255, 60, 40, 255),
            new Color(200, 200, 220, 255)
        };

        for (int i = 0; i < particleCount; i++)
        {
            float angle = (float)(rng.NextDouble() * Math.PI * 2.0);
            float speed = 90f + (float)rng.NextDouble() * 260f;
            float lifetime = 0.5f + (float)rng.NextDouble() * 0.9f;

            burst.Particles.Add(new CombatExplosionParticle
            {
                Position = arenaPosition,
                Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed,
                Lifetime = lifetime,
                MaxLifetime = lifetime,
                Size = 2f + (float)rng.NextDouble() * 5f,
                Color = palette[rng.Next(palette.Length)]
            });
        }

        _explosionBursts.Add(burst);
    }

    private void UpdateExplosions(float deltaTime)
    {
        for (int b = _explosionBursts.Count - 1; b >= 0; b--)
        {
            CombatExplosionBurst burst = _explosionBursts[b];
            burst.FlashTimer = MathF.Max(0f, burst.FlashTimer - deltaTime);

            for (int i = burst.Particles.Count - 1; i >= 0; i--)
            {
                CombatExplosionParticle particle = burst.Particles[i];
                particle.Position += particle.Velocity * deltaTime;
                particle.Velocity *= MathF.Exp(-2.8f * deltaTime);
                particle.Lifetime -= deltaTime;

                if (!particle.IsAlive)
                {
                    burst.Particles.RemoveAt(i);
                }
            }

            if (burst.IsFinished)
            {
                _explosionBursts.RemoveAt(b);
            }
        }
    }

    private void DrawExplosions(int centerX, int centerY)
    {
        for (int b = 0; b < _explosionBursts.Count; b++)
        {
            CombatExplosionBurst burst = _explosionBursts[b];
            int sx = centerX + (int)burst.Position.X;
            int sy = centerY + (int)burst.Position.Y;

            if (burst.FlashTimer > 0f)
            {
                float flashT = burst.FlashTimer / ExplosionFlashDuration;
                int flashRadius = (int)(18f + (1f - flashT) * 42f);
                byte flashAlpha = (byte)(220 * flashT);
                Raylib.DrawCircle(sx, sy, flashRadius, new Color((byte)255, (byte)230, (byte)160, flashAlpha));
                Raylib.DrawCircle(sx, sy, flashRadius * 0.55f, new Color((byte)255, (byte)120, (byte)50, (byte)(180 * flashT)));
            }

            for (int i = 0; i < burst.Particles.Count; i++)
            {
                CombatExplosionParticle particle = burst.Particles[i];
                float lifeT = particle.Lifetime / particle.MaxLifetime;
                int px = centerX + (int)particle.Position.X;
                int py = centerY + (int)particle.Position.Y;
                byte alpha = (byte)(255 * lifeT);
                Color color = new Color(particle.Color.R, particle.Color.G, particle.Color.B, alpha);
                int radius = Math.Max(1, (int)(particle.Size * (0.5f + lifeT * 0.5f)));
                Raylib.DrawCircle(px, py, radius, color);
            }
        }
    }

    private static void DrawArenaBoundary(int centerX, int centerY, float arenaRadius)
    {
        Raylib.DrawCircleLines(centerX, centerY, arenaRadius, new Color(80, 90, 130, 180));
    }

    private void DrawProjectiles(int centerX, int centerY)
    {
        for (int i = 0; i < _projectiles.Count; i++)
        {
            CombatProjectile proj = _projectiles[i];
            int sx = centerX + (int)proj.Position.X;
            int sy = centerY + (int)proj.Position.Y;
            Color color = proj.IsPlayerOwned
                ? new Color(120, 220, 255, 255)
                : new Color(255, 100, 90, 255);
            Raylib.DrawCircle(sx, sy, 3, color);
        }
    }

    private void DrawEnemies(int centerX, int centerY)
    {
        for (int i = 0; i < _enemies.Count; i++)
        {
            CombatEnemy enemy = _enemies[i];
            if (!enemy.IsAlive)
            {
                continue;
            }

            DrawEnemyShip(enemy, centerX, centerY);
        }
    }

    private static void DrawEnemyShip(CombatEnemy enemy, int centerX, int centerY)
    {
        int sx = centerX + (int)enemy.Position.X;
        int sy = centerY + (int)enemy.Position.Y;

        Vector2 center = new Vector2(sx, sy);
        float s = 1.1f;
        float rot = enemy.Rotation;

        Vector2[] basePoints =
        {
            new Vector2(0, -24f * s),
            new Vector2(-18f * s, 14f * s),
            new Vector2(18f * s, 14f * s)
        };

        Vector2[] points = new Vector2[3];
        for (int i = 0; i < 3; i++)
        {
            points[i] = RotateAround(basePoints[i], rot) + center;
        }

        Color fill = new Color(180, 60, 70, 255);
        Color outline = new Color(255, 140, 120, 255);
        Raylib.DrawTriangle(points[0], points[1], points[2], fill);
        Raylib.DrawTriangleLines(points[0], points[1], points[2], outline);

        float hullPct = enemy.Hull / EnemyHullMax;
        int barY = sy - 36;
        DrawHealthBar(sx - HealthBarWidth / 2, barY, HealthBarWidth, HealthBarHeight, hullPct, Color.RED);
    }

    private static void DrawPlayerHealthBars(int shipCenterX, int shipCenterY, IShip ship)
    {
        int barX = shipCenterX - HealthBarWidth / 2;
        int armorBarY = shipCenterY - 44;
        int shieldBarY = armorBarY - HealthBarHeight - HealthBarGap;

        DrawHealthBar(
            barX,
            shieldBarY,
            HealthBarWidth,
            HealthBarHeight,
            ship.GetShieldFillFraction(),
            new Color(80, 200, 255, 255));
        DrawHealthBar(
            barX,
            armorBarY,
            HealthBarWidth,
            HealthBarHeight,
            ship.GetHullFillFraction(),
            new Color(210, 170, 90, 255));
    }

    private static void DrawHealthBar(int x, int y, int width, int height, float fill01, Color fillColor)
    {
        Raylib.DrawRectangle(x, y, width, height, new Color(40, 40, 50, 255));
        if (fill01 <= 0f)
        {
            return;
        }

        int fillW = Math.Max(1, (int)(width * Math.Clamp(fill01, 0f, 1f)));
        Raylib.DrawRectangle(x, y, fillW, height, fillColor);
    }

    private void DrawCombatHud(int viewWidth, int viewHeight, IShip ship)
    {
        const int frameThickness = 20;
        Color frameColor = new Color(40, 40, 45, 255);

        Raylib.DrawRectangle(0, 0, viewWidth, frameThickness, frameColor);
        Raylib.DrawRectangle(0, viewHeight - frameThickness, viewWidth, frameThickness, frameColor);
        Raylib.DrawRectangle(0, 0, frameThickness, viewHeight, frameColor);
        Raylib.DrawRectangle(viewWidth - frameThickness, 0, frameThickness, viewHeight, frameColor);

        UiText.DrawText("COMBAT", 30, 30, 24, new Color(255, 120, 100, 255));

        int aliveCount = 0;
        for (int i = 0; i < _enemies.Count; i++)
        {
            if (_enemies[i].IsAlive)
            {
                aliveCount++;
            }
        }

        UiText.DrawText($"Hostiles: {aliveCount}", 30, 58, 18, Color.LIGHTGRAY);
        UiText.DrawText(
            $"Shields: {ship.ShieldStrength:F0}   Armor: {ship.HullStrength:F0}",
            30,
            80,
            18,
            Color.SKYBLUE);

        string statusLine;
        Color statusColor;
        if (IsVictory)
        {
            statusLine = "VICTORY — +500 credits. Press ESC to disengage.";
            statusColor = new Color(120, 255, 160, 255);
        }
        else if (IsDefeat)
        {
            statusLine = "ARMOR BREACHED — Press ESC to retreat.";
            statusColor = new Color(255, 100, 100, 255);
        }
        else
        {
            statusLine = "WASD | A/D | SPACE fire | ESC leave | H: restore ship (debug)";
            statusColor = Color.YELLOW;
        }

        UiText.DrawText(statusLine, 24, viewHeight - frameThickness + 4, 14, statusColor);
    }

    private void ClampToArena(ref Vector2 position)
    {
        float distSq = position.LengthSquared();
        float maxSq = _arenaRadius * _arenaRadius;
        if (distSq > maxSq && distSq > 0.0001f)
        {
            position = Vector2.Normalize(position) * _arenaRadius;
        }
    }

    private static Vector2 GetForward(float rotation)
    {
        return new Vector2(MathF.Sin(rotation), -MathF.Cos(rotation));
    }

    private static float RotateToward(float current, float target, float maxDelta)
    {
        float diff = NormalizeAngle(target - current);
        if (MathF.Abs(diff) <= maxDelta)
        {
            return target;
        }

        return current + MathF.Sign(diff) * maxDelta;
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > MathF.PI)
            angle -= MathF.PI * 2f;
        while (angle < -MathF.PI)
            angle += MathF.PI * 2f;
        return angle;
    }

    private static Vector2 RotateAround(Vector2 point, float angle)
    {
        float cos = MathF.Cos(angle);
        float sin = MathF.Sin(angle);
        float dx = point.X;
        float dy = point.Y;
        return new Vector2(dx * cos - dy * sin, dx * sin + dy * cos);
    }
}
