namespace StarflightGame.Constants;

/// <summary>Shared layout measurements for the main window (right-hand panel, etc.).</summary>
public static class LayoutConstants
{
    public const int RightPanelWidth = 250;

    /// <summary>Width of the main content area (full window width minus the right-hand panel).</summary>
    public static int MainViewWidth(int fullWindowWidth) => fullWindowWidth - RightPanelWidth;

    public const int RightPanelPadding = 15;
    public const int RightPanelLineSpacing = 25;
    public const int StatusPanelFontSize = 18;

    /// <summary>Height of the star-centered overview map in star system interior view.</summary>
    public const int StarSystemRightPanelMapHeight = 200;
}
