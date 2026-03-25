using Microsoft.Extensions.DependencyInjection;
using StarflightGame.Views;

namespace StarflightGame.Registry;

public static class GameRegistry
{
    public static IServiceCollection RegisterGame(this IServiceCollection serviceCollection) =>
        serviceCollection
            .AddSingleton<IShip, Ship>()
            .AddSingleton<IRightPanel, RightPanel>()
            .AddSingleton<IGame, Game>();
}