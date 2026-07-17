using BetterGenshinImpact.Core.Simulator.Extensions;
using BetterGenshinImpact.GameTask.Model.Area;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace BetterGenshinImpact.GameTask.Common;

public interface ITaskControlPlatform
{
    ILogger Logger { get; }
    double DpiScale { get; }
    void EnsureGameActive();
    void ReleasePressedInputs();
    void SimulateAction(GIActions action, KeyType keyType);
    bool IsActionKeyDown(GIActions action);
    void MoveMouseBy(int x, int y);
    void LeftButtonDown();
    void LeftButtonUp();
    void RightButtonUp();
    void MiddleButtonClick();
    void PressEscape();
    ImageRegion CaptureToRectArea(bool forceNew);
}

public static class TaskControlPlatform
{
    private static ITaskControlPlatform? _current;

    public static ITaskControlPlatform Current => Volatile.Read(ref _current)
        ?? throw new InvalidOperationException("TaskControl platform has not been composed.");

    public static void Configure(ITaskControlPlatform platform)
    {
        ArgumentNullException.ThrowIfNull(platform);
        if (Interlocked.CompareExchange(ref _current, platform, null) is not null)
            throw new InvalidOperationException("TaskControl platform has already been configured.");
    }
}
