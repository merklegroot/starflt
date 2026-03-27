using Microsoft.Extensions.DependencyInjection;
using StarflightGame.Views;
using StarflightGame.Views.StarMap;

namespace StarflightGame.Registry;

public static class GameRegistry
{
    public static IServiceCollection RegisterGame(this IServiceCollection serviceCollection) =>
        serviceCollection
            .AddSingleton<IShip, Ship>()
            .AddSingleton<IGameMenu, GameMenu>()
            .AddSingleton<IStatusPanel, StatusPanel>()
            .AddSingleton<IStarMapView, StarMapView>()
            .AddSingleton<IParallaxStarfield, ParallaxStarfield>()
            .AddSingleton<IPlanetView, PlanetView>()
            .AddSingleton<ICanopyStarSystemView, CanopyStarSystemView>()
            .AddSingleton<IRightPanel, RightPanel>()
            .AddSingleton<IGame, Game>();
}