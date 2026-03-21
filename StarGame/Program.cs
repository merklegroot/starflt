using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StarflightGame.Registry;

namespace StarflightGame;

class Program
{
    static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.RegisterGame();
        using var host = builder.Build();
        
        var game = host.Services.GetRequiredService<IGame>();
        game.Run();
    }
}
