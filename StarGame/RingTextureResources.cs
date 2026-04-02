using Raylib_cs;

namespace StarflightGame;

/// <summary>
/// Loads optional ring textures shipped under <c>Textures/</c> (see ASSETS.md for license).
/// </summary>
internal static class RingTextureResources
{
    private static Texture2D _saturnRingAlpha;
    private static bool _loaded;
    private static bool _loadFailed;

    private const string SaturnRingRelativePath = "Textures/solarsystemscope/saturn_ring_alpha.png";

    /// <summary>
    /// Solar System Scope Saturn ring alpha (CC BY 4.0). Used for every ringed planet while this is the only ring texture shipped.
    /// </summary>
    public static bool TryGetSaturnRingAlpha(out Texture2D texture)
    {
        texture = default;

        if (_loadFailed)
        {
            return false;
        }

        if (!_loaded)
        {
            string path = Path.Combine(AppContext.BaseDirectory, SaturnRingRelativePath);

            if (!File.Exists(path))
            {
                _loadFailed = true;
                return false;
            }

            _saturnRingAlpha = Raylib.LoadTexture(path);
            Raylib.SetTextureFilter(_saturnRingAlpha, TextureFilter.TEXTURE_FILTER_BILINEAR);
            Raylib.SetTextureWrap(_saturnRingAlpha, TextureWrap.TEXTURE_WRAP_CLAMP);
            _loaded = true;
        }

        texture = _saturnRingAlpha;
        return true;
    }
}
