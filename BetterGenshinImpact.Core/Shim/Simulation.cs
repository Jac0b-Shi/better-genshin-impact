using BetterGenshinImpact.Platform.Abstractions;

namespace BetterGenshinImpact.Core.Simulator;

/// <summary>
/// TEMPORARY VERIFICATION FACADE: delegates static Simulation calls to PlatformServices.Input.
/// </summary>
public class Simulation
{
    public static IInputBackend InputBackend => PlatformServices.Input;

    public static readonly SendInputFacade SendInput = new();
    public static MouseEventFacade MouseEvent { get; } = new();

    public static void ReleaseAllKey()
    {
        throw new NotSupportedException("ReleaseAllKey is not implemented for the cross-platform Core.");
    }
}

public class SendInputFacade
{
    public KeyboardFacade Keyboard { get; } = new();
    public MouseFacade Mouse { get; } = new();
}

public class KeyboardFacade
{
    public void KeyPress(BgiKey key) => Simulation.InputBackend.KeyPress(key);
    public void KeyDown(BgiKey key) => Simulation.InputBackend.KeyDown(key);
    public void KeyUp(BgiKey key) => Simulation.InputBackend.KeyUp(key);
}

public class MouseFacade
{
    public void VerticalScroll(int clicks) => Simulation.InputBackend.Scroll(clicks);

    public MouseFacade MoveMouseTo(int screenX, int screenY)
    {
        Simulation.InputBackend.MoveMouseTo(screenX, screenY);
        return this;
    }

    public MouseFacade MoveMouseBy(int deltaX, int deltaY)
    {
        Simulation.InputBackend.MoveMouseBy(deltaX, deltaY);
        return this;
    }

    public MouseFacade LeftButtonDown()
    {
        Simulation.InputBackend.LeftButtonDown();
        return this;
    }

    public MouseFacade LeftButtonUp()
    {
        Simulation.InputBackend.LeftButtonUp();
        return this;
    }

    public MouseFacade LeftButtonClick()
    {
        throw new NotSupportedException(
            "LeftButtonClick() without coordinates is not supported. Use DesktopRegion.DesktopRegionClick() instead.");
    }

    /// <summary>Fluent sleep — matches Fischless chain API.</summary>
    public MouseFacade Sleep(int milliseconds)
    {
        Thread.Sleep(milliseconds);
        return this;
    }
}

public class MouseEventFacade
{
    // Placeholder for MouseEventSimulator compatibility
}
