using Microsoft.Extensions.DependencyInjection;
using StarflightGame.Combat;
using StarflightGame.Mining;
using StarflightGame.Views;
using StarflightGame.Views.StarMap;

namespace StarflightGame.Registry;

public static class GameRegistry
{
    public static IServiceCollection RegisterGame(this IServiceCollection serviceCollection) =>
        serviceCollection
            .AddSingleton<IPlanetMiningRigStore, PlanetMiningRigStore>()
            .AddSingleton<IShip, Ship>()
            .AddSingleton<IGameMenu, GameMenu>()
            .AddSingleton<IStatusPanel, StatusPanel>()
            .AddSingleton<IResourceLoader, ResourceLoader>()
            .AddSingleton<IStarMapView, StarMapView>()
            .AddSingleton<IParallaxStarfield, ParallaxStarfield>()
            .AddSingleton<IPlanetView, PlanetView>()
            .AddSingleton<ICanopyStarSystemView, CanopyStarSystemView>()
            .AddSingleton<IStarSystemInteriorView, StarSystemInteriorView>()
            .AddSingleton<ICombatView, CombatView>()
            .AddSingleton<IRightPanel, RightPanel>()
            .AddSingleton<IGame, Game>();
}