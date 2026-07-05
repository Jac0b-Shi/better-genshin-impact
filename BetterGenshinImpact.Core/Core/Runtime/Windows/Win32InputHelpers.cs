using BetterGenshinImpact.Platform.Abstractions;

namespace BetterGenshinImpact.Core.Runtime.Windows;

/// <summary>
/// Pure platform-agnostic helpers for Win32 coordinate conversion and key mapping.
/// No Windows/WPF/Vanara dependencies — usable from any platform for testing.
/// </summary>
public static class Win32InputHelpers
{
    /// <summary>
    /// Map BgiKey to the Win32 virtual-key code (int).
    /// These int values match the User32.VK enum from Vanara.PInvoke.
    /// </summary>
    public static int MapBgiKeyToVk(BgiKey key)
    {
        return key switch
        {
            BgiKey.F => 0x46,          // VK_F
            BgiKey.Escape => 0x1B,     // VK_ESCAPE
            BgiKey.Space => 0x20,      // VK_SPACE
            BgiKey.Enter => 0x0D,      // VK_RETURN
            BgiKey.Tab => 0x09,        // VK_TAB
            BgiKey.W => 0x57,          // 'W'
            BgiKey.A => 0x41,          // 'A'
            BgiKey.S => 0x53,          // 'S'
            BgiKey.D => 0x44,          // 'D'
            BgiKey.LeftShift => 0xA0,  // VK_LSHIFT
            BgiKey.LeftControl => 0xA2,// VK_LCONTROL
            BgiKey.LeftAlt => 0xA4,    // VK_LMENU
            _ => throw new ArgumentOutOfRangeException(
                nameof(key), key, "BgiKey has no Win32 VK mapping.")
        };
    }

    /// <summary>
    /// Convert screen-pixel coordinates (relative to virtual desktop origin)
    /// to normalized 0–65535 absolute coordinates for Win32 SendInput.
    /// </summary>
    /// <param name="screenX">Screen-pixel X (virtual-desktop-relative).</param>
    /// <param name="screenY">Screen-pixel Y (virtual-desktop-relative).</param>
    /// <param name="virtualLeft">Virtual desktop left edge (may be negative).</param>
    /// <param name="virtualTop">Virtual desktop top edge (may be negative).</param>
    /// <param name="virtualWidth">Virtual desktop total width.</param>
    /// <param name="virtualHeight">Virtual desktop total height.</param>
    /// <returns>Normalized (x, y) in 0..65535 range.</returns>
    public static (int nx, int ny) ScreenToNormalized(
        int screenX, int screenY,
        int virtualLeft, int virtualTop,
        int virtualWidth, int virtualHeight)
    {
        if (virtualWidth <= 1 || virtualHeight <= 1)
            throw new ArgumentException(
                "Virtual desktop dimensions must be > 1 for normalized coordinate conversion.");

        // Subtract virtual origin so coordinates are relative to virtual desktop (0,0).
        int relX = screenX - virtualLeft;
        int relY = screenY - virtualTop;

        // Normalize to 0..65535. Subtract 1 from denominator: Win32 convention
        // maps virtualWidth-1 → 65535, not virtualWidth → 65535.
        int nx = (int)((long)relX * 65535 / (virtualWidth - 1));
        int ny = (int)((long)relY * 65535 / (virtualHeight - 1));

        // Clamp to valid range (edge rounding).
        nx = Math.Clamp(nx, 0, 65535);
        ny = Math.Clamp(ny, 0, 65535);

        return (nx, ny);
    }

    /// <summary>
    /// Number of WHEEL_DELTA units per logical scroll click (Windows convention).
    /// </summary>
    public const int WheelDeltaPerClick = 120;

    /// <summary>
    /// Convert logical scroll clicks to Win32 mouse wheel data value.
    /// Positive = wheel up (same direction as Windows VerticalScroll).
    /// </summary>
    public static int ScrollClicksToWheelData(int clicks)
    {
        return clicks * WheelDeltaPerClick;
    }
}
