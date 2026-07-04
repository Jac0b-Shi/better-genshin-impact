using BetterGenshinImpact.Platform.Abstractions;

namespace BetterGenshinImpact;

/// <summary>
/// Static gateway to platform services. Initialized by the host before starting the dispatcher.
/// </summary>
public static class PlatformServices
{
    public static IInputBackend Input { get; set; } = null!;
    public static IUserInteractionService? UserInteraction { get; set; }
}
