using System.Numerics;
using Raylib_cs;

namespace StarflightGame;

/// <summary>
/// Hybrid keyboard + gamepad input. Call <see cref="Initialize"/> once after <see cref="Raylib.InitWindow"/>.
/// </summary>
public static class InputManager
{
    public const int DefaultGamepad = 0;

    private const float AxisDeadzone = 0.2f;

    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        LoadSteamDeckMappings();
    }

    public static void Update()
    {
    }

    public static bool IsGamepadConnected => Raylib.IsGamepadAvailable(DefaultGamepad);

    public static bool IsConfirmPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_ENTER)
        || GamepadPressed(GamepadButton.GAMEPAD_BUTTON_RIGHT_FACE_DOWN);

    public static bool IsBackPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE)
        || GamepadPressed(GamepadButton.GAMEPAD_BUTTON_RIGHT_FACE_RIGHT);

    public static bool IsMenuUpPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_UP)
        || GamepadPressed(GamepadButton.GAMEPAD_BUTTON_LEFT_FACE_UP);

    public static bool IsMenuDownPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_DOWN)
        || GamepadPressed(GamepadButton.GAMEPAD_BUTTON_LEFT_FACE_DOWN);

    public static bool IsMenuLeftPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_LEFT)
        || GamepadPressed(GamepadButton.GAMEPAD_BUTTON_LEFT_FACE_LEFT);

    public static bool IsMenuRightPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_RIGHT)
        || GamepadPressed(GamepadButton.GAMEPAD_BUTTON_LEFT_FACE_RIGHT);

    public static bool IsQuitPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_X)
        || GamepadPressed(GamepadButton.GAMEPAD_BUTTON_MIDDLE);

    public static bool IsEnterStarSystemPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_SPACE)
        || GamepadPressed(GamepadButton.GAMEPAD_BUTTON_RIGHT_FACE_DOWN);

    public static bool IsCombatPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_C)
        || GamepadPressed(GamepadButton.GAMEPAD_BUTTON_RIGHT_FACE_LEFT);

    public static bool IsManifestPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_M)
        || GamepadPressed(GamepadButton.GAMEPAD_BUTTON_MIDDLE_LEFT);

    public static bool IsRefuelPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_R)
        || GamepadPressed(GamepadButton.GAMEPAD_BUTTON_RIGHT_FACE_UP);

    public static bool IsDebugHealPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_H)
        || GamepadPressed(GamepadButton.GAMEPAD_BUTTON_LEFT_TRIGGER_1);

    public static bool IsShipStatusPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_I)
        || GamepadPressed(GamepadButton.GAMEPAD_BUTTON_MIDDLE_RIGHT);

    public static bool IsPlanetListTogglePressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_P)
        || GamepadPressed(GamepadButton.GAMEPAD_BUTTON_LEFT_THUMB);

    public static bool IsWarpPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_TAB)
        || GamepadPressed(GamepadButton.GAMEPAD_BUTTON_RIGHT_TRIGGER_1);

    public static bool IsPreviousSystemPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_LEFT_BRACKET)
        || Raylib.IsKeyPressed(KeyboardKey.KEY_COMMA)
        || GamepadPressed(GamepadButton.GAMEPAD_BUTTON_LEFT_TRIGGER_2);

    public static bool IsNextSystemPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.KEY_RIGHT_BRACKET)
        || Raylib.IsKeyPressed(KeyboardKey.KEY_PERIOD)
        || GamepadPressed(GamepadButton.GAMEPAD_BUTTON_RIGHT_TRIGGER_2);

    public static bool IsFireHeld() =>
        Raylib.IsKeyDown(KeyboardKey.KEY_SPACE)
        || (IsGamepadConnected
            && (Raylib.IsGamepadButtonDown(DefaultGamepad, GamepadButton.GAMEPAD_BUTTON_RIGHT_FACE_DOWN)
                || Raylib.GetGamepadAxisMovement(DefaultGamepad, GamepadAxis.GAMEPAD_AXIS_RIGHT_TRIGGER) > AxisDeadzone));

    public static float GetTurnAxis()
    {
        float turn = 0f;

        if (Raylib.IsKeyDown(KeyboardKey.KEY_A) || Raylib.IsKeyDown(KeyboardKey.KEY_LEFT))
        {
            turn -= 1f;
        }

        if (Raylib.IsKeyDown(KeyboardKey.KEY_D) || Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT))
        {
            turn += 1f;
        }

        if (IsGamepadConnected)
        {
            float stickX = ApplyDeadzone(Raylib.GetGamepadAxisMovement(DefaultGamepad, GamepadAxis.GAMEPAD_AXIS_LEFT_X));
            turn = Math.Clamp(turn + stickX, -1f, 1f);
        }

        return turn;
    }

    public static void GetThrustHeld(out bool forward, out bool reverse)
    {
        forward = Raylib.IsKeyDown(KeyboardKey.KEY_W) || Raylib.IsKeyDown(KeyboardKey.KEY_UP);
        reverse = Raylib.IsKeyDown(KeyboardKey.KEY_S) || Raylib.IsKeyDown(KeyboardKey.KEY_DOWN);

        if (!IsGamepadConnected)
        {
            return;
        }

        float stickY = ApplyDeadzone(Raylib.GetGamepadAxisMovement(DefaultGamepad, GamepadAxis.GAMEPAD_AXIS_LEFT_Y));
        if (stickY < -AxisDeadzone)
        {
            forward = true;
        }

        if (stickY > AxisDeadzone)
        {
            reverse = true;
        }

        float rightTrigger = Raylib.GetGamepadAxisMovement(DefaultGamepad, GamepadAxis.GAMEPAD_AXIS_RIGHT_TRIGGER);
        float leftTrigger = Raylib.GetGamepadAxisMovement(DefaultGamepad, GamepadAxis.GAMEPAD_AXIS_LEFT_TRIGGER);
        if (rightTrigger > AxisDeadzone)
        {
            forward = true;
        }

        if (leftTrigger > AxisDeadzone)
        {
            reverse = true;
        }
    }

    /// <summary>Pan offset for star map camera (keyboard arrows/WASD + left stick).</summary>
    public static Vector2 GetMapPanAxis()
    {
        float x = 0f;
        float y = 0f;

        if (Raylib.IsKeyDown(KeyboardKey.KEY_W) || Raylib.IsKeyDown(KeyboardKey.KEY_UP))
        {
            y -= 1f;
        }

        if (Raylib.IsKeyDown(KeyboardKey.KEY_S) || Raylib.IsKeyDown(KeyboardKey.KEY_DOWN))
        {
            y += 1f;
        }

        if (Raylib.IsKeyDown(KeyboardKey.KEY_A) || Raylib.IsKeyDown(KeyboardKey.KEY_LEFT))
        {
            x -= 1f;
        }

        if (Raylib.IsKeyDown(KeyboardKey.KEY_D) || Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT))
        {
            x += 1f;
        }

        if (IsGamepadConnected)
        {
            x += ApplyDeadzone(Raylib.GetGamepadAxisMovement(DefaultGamepad, GamepadAxis.GAMEPAD_AXIS_LEFT_X));
            y += ApplyDeadzone(Raylib.GetGamepadAxisMovement(DefaultGamepad, GamepadAxis.GAMEPAD_AXIS_LEFT_Y));
        }

        if (x == 0f && y == 0f)
        {
            return Vector2.Zero;
        }

        return Vector2.Normalize(new Vector2(x, y));
    }

    /// <summary>Zoom delta per frame from mouse wheel, shoulder buttons, or right stick Y.</summary>
    public static float GetMapZoomWheelDelta()
    {
        float delta = Raylib.GetMouseWheelMove();

        if (IsGamepadConnected)
        {
            if (Raylib.IsGamepadButtonDown(DefaultGamepad, GamepadButton.GAMEPAD_BUTTON_LEFT_TRIGGER_1))
            {
                delta -= 0.08f;
            }

            if (Raylib.IsGamepadButtonDown(DefaultGamepad, GamepadButton.GAMEPAD_BUTTON_RIGHT_TRIGGER_1))
            {
                delta += 0.08f;
            }

            float rightStickY = ApplyDeadzone(Raylib.GetGamepadAxisMovement(DefaultGamepad, GamepadAxis.GAMEPAD_AXIS_RIGHT_Y));
            if (MathF.Abs(rightStickY) > AxisDeadzone)
            {
                delta += rightStickY * 0.04f;
            }
        }

        return delta;
    }

    private static bool GamepadPressed(GamepadButton button) =>
        IsGamepadConnected && Raylib.IsGamepadButtonPressed(DefaultGamepad, button);

    private static float ApplyDeadzone(float value) =>
        MathF.Abs(value) < AxisDeadzone ? 0f : value;

    private static void LoadSteamDeckMappings()
    {
        const string mappings = """
            03000000de280000ff11000001000000,Steam Virtual Gamepad,a:b0,b:b1,x:b2,y:b3,back:b4,start:b6,leftstick:b7,rightstick:b8,leftshoulder:b9,rightshoulder:b10,dpdown:h0.4,dpleft:h0.8,dpright:h0.2,dpup:h0.1,leftx:a0,lefty:a1,rightx:a2,righty:a3,lefttrigger:a4,righttrigger:a5,platform:Linux
            028e04504c0500000000000000000000,Steam Deck,a:b0,b:b1,x:b2,y:b3,back:b4,guide:b5,start:b6,leftstick:b7,rightstick:b8,leftshoulder:b9,rightshoulder:b10,dpdown:b15,dpleft:b16,dpright:b17,dpup:b18,leftx:a0,lefty:a1,rightx:a2,righty:a3,lefttrigger:a4,righttrigger:a5,platform:Linux
            03000000750e00000603000001000000,Steam Controller,a:b0,b:b1,y:b2,x:b3,guide:b4,back:b5,start:b6,leftstick:b7,rightstick:b8,leftshoulder:b9,rightshoulder:b10,dpup:b11,dpdown:b12,dpleft:b13,dpright:b14,leftx:a0,lefty:a1,rightx:a2,righty:a3,lefttrigger:a4,righttrigger:a5,platform:Linux
            """;

        Raylib.SetGamepadMappings(mappings);
    }
}
