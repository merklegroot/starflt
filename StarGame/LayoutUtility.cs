using StarflightGame.Constants;

namespace StarflightGame;

/// <summary>Layout helpers derived from window dimensions (not compile-time constants).</summary>
public static class LayoutUtility
{
    /// <summary>Width of the main content area (full window width minus the right-hand panel).</summary>
    public static int MainViewWidth(int fullWindowWidth) => fullWindowWidth - LayoutConstants.RightPanelWidth;
}
