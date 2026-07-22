using BetterGenshinImpact.GameTask.AutoMusicGame;
using BetterGenshinImpact.GameTask.Model;
using Microsoft.Extensions.Logging;

namespace BetterGenshinImpact.Core.Host.Runtime;

public sealed class MacAutoAlbumRuntimePlatform(
    Func<ISystemInfo> systemInfo,
    ILogger<AutoAlbumTask> logger) : IAutoAlbumRuntimePlatform
{
    public ISystemInfo SystemInfo => systemInfo();
    public bool PropagateTaskExceptions => true;
    public ILogger<AutoAlbumTask> Logger { get; } = logger;

    public void Notify(
        AutoAlbumNotification notification, string message, Exception? exception = null)
    {
        if (notification == AutoAlbumNotification.Error)
            Logger.LogError(exception, "{Message}", message);
        else
            Logger.LogInformation("AutoAlbum {Notification}: {Message}", notification, message);
    }
}
