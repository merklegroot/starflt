using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raylib_cs;

namespace StarflightGame;

class Program
{
    static void Main(string[] args)
    {
        const int screenWidth = 1280;
        const int screenHeight = 720;

        Raylib.InitWindow(screenWidth, screenHeight, "Starflight");
        Raylib.SetTargetFPS(60);

        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddSingleton<Game>(_ => new Game(screenWidth, screenHeight));
        using var host = builder.Build();
        var game = host.Services.GetRequiredService<Game>();
        
        while (!game.ShouldExit && !WindowShouldClose())
        {
            game.Update();
            game.Draw();
        }

        game.UnloadResources();
        Raylib.CloseWindow();
    }
    
    private static bool WindowShouldClose()
    {
        // Check if window close was requested (like clicking the X button)
        // but ignore if it was triggered by the Escape key
        bool closeRequested = Raylib.WindowShouldClose();
        
        // If close is requested and it's because of Escape key, ignore it
        if (closeRequested && Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE))
        {
            return false;
        }
        
        return closeRequested;
    }
}
