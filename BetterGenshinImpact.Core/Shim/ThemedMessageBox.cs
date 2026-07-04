using BetterGenshinImpact.Platform.Abstractions;

namespace BetterGenshinImpact.View.Windows;

/// <summary>
/// Thin platform facade: delegates WPF MessageBox to IUserInteractionService.
/// </summary>
public static class ThemedMessageBox
{
    public static IUserInteractionService? UserInteraction { get; set; }

    public static void Error(string message)
    {
        UserInteraction?.ShowError(message);
    }

    public static void Warning(string message)
    {
        UserInteraction?.ShowWarning(message);
    }
}
