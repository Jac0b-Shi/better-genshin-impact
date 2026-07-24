using BetterGenshinImpact.Core.Host.Runtime;
using BetterGenshinImpact.Verification.Framework;

namespace BetterGenshinImpact.Core.Host.Fast.Verification;

public sealed class SchedulerStatusSuite : IVerificationSuite
{
    public string Name => "scheduler-status";

    public Task RunAsync(VerificationContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tracker = new SchedulerStatusTracker();
        context.Require(
            tracker.Snapshot() == new SchedulerStatusSnapshot(null, "idle", null, null),
            "Scheduler status did not start idle.");

        const string taskId = "task-a";
        context.Require(
            tracker.Start(taskId, "Group A") ==
            new SchedulerStatusSnapshot(taskId, "running", "Group A", null),
            "Scheduler status did not expose the running task.");
        context.Require(
            tracker.Transition(taskId, "paused").State == "paused" &&
            tracker.Transition(taskId, "running").State == "running" &&
            tracker.Transition(taskId, "stopping").State == "stopping" &&
            tracker.Transition(taskId, "cancelled").State == "cancelled",
            "Scheduler status did not preserve its lifecycle transitions.");

        tracker.Start("task-b", "Group B");
        var failed = tracker.Transition("task-b", "failed", "failure");
        context.Require(
            failed.State == "failed" && failed.Error == "failure",
            "Scheduler status did not preserve terminal error details.");

        var staleTransitionRejected = false;
        try
        {
            tracker.Transition(taskId, "completed");
        }
        catch (InvalidOperationException)
        {
            staleTransitionRejected = true;
        }
        context.Require(
            staleTransitionRejected,
            "Scheduler status accepted a stale task transition.");

        var invalidStateRejected = false;
        try
        {
            tracker.Transition("task-b", "unknown");
        }
        catch (ArgumentException)
        {
            invalidStateRejected = true;
        }
        context.Require(
            invalidStateRejected,
            "Scheduler status accepted an unsupported lifecycle state.");
        return Task.CompletedTask;
    }
}
