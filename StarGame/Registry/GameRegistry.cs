using Microsoft.Extensions.DependencyInjection;

namespace StarflightGame.Registry;

public static class GameRegistry
{
    public static IServiceCollection RegisterGame(this IServiceCollection serviceCollection) =>
        serviceCollection.AddSingleton<IGame, Game>();
}