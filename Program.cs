using Raylib_cs;
using System.Numerics;

namespace StarflightGame;

class Program
{
    static void Main(string[] args)
    {
        const int screenWidth = 1280;
        const int screenHeight = 720;

        Raylib.InitWindow(screenWidth, screenHeight, "Starflight");
        Raylib.SetTargetFPS(60);

        var game = new Game(screenWidth, screenHeight);
        
        while (!Raylib.WindowShouldClose())
        {
            game.Update();
            game.Draw();
        }

        Raylib.CloseWindow();
    }
}
