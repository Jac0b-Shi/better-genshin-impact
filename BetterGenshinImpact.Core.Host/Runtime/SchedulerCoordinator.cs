using BetterGenshinImpact.Core.Host.Transport;
using BetterGenshinImpact.Core.Script;
using BetterGenshinImpact.Core.Script.Group;
using BetterGenshinImpact.GameTask;
using BetterGenshinImpact.GameTask.TaskProgress;
using BetterGenshinImpact.Service;
using Newtonsoft.Json.Linq;

namespace BetterGenshinImpact.Core.Host.Runtime;

/// <summary>Owns one real upstream ScriptService.RunMulti execution at a time.</summary>
public sealed class SchedulerCoordinator(
    RuntimeLayout layout,
    PlatformCallbackChannel callbacks,
    string sessionToken,
    CancellationToken hostCancellationToken)
{
    private readonly object _sync = new();
    private string? _taskId;
    private Task? _execution;
    private CancellationTokenSource? _operationCancellation;

    public object Run(string groupName)
    {
        var path = ResolveGroup(groupName);
        var group = ScriptGroup.FromJson(File.ReadAllText(path));
        ApplyNextProject(group);
        if (group.Projects.Count == 0)
            throw new InvalidDataException($"Script group '{groupName}' contains no projects.");
        return Start(group.Name, _ => RunGroupAsync(group));
    }

    public object RunGroups(IReadOnlyList<string> groupNames)
    {
        ArgumentNullException.ThrowIfNull(groupNames);
        if (groupNames.Count == 0)
            throw new ArgumentException("At least one script group name is required.", nameof(groupNames));

        var groups = groupNames
            .Select(groupName =>
            {
                var path = ResolveGroup(groupName);
                var group = ScriptGroup.FromJson(File.ReadAllText(path));
                ApplyNextProject(group);
                if (group.Projects.Count == 0)
                    throw new InvalidDataException($"Script group '{groupName}' contains no projects.");
                return group;
            })
            .ToArray();
        var displayName = string.Join(",", groups.Select(group => group.Name));
        return Start(displayName, cancellationToken => RunGroupsAsync(groups, cancellationToken));
    }

    public object RunProject(ScriptGroupProject project, string displayName)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Task display name cannot be empty.", nameof(displayName));
        return Start(displayName, _ =>
            new ScriptService().RunMulti([project], displayName));
    }

    private object Start(string displayName, Func<CancellationToken, Task> operation)
    {
        lock (_sync)
        {
            if (_execution is { IsCompleted: false })
                throw new InvalidOperationException($"Scheduler task '{_taskId}' is already running.");
            _operationCancellation?.Dispose();
            _operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(hostCancellationToken);
            RunnerContext.Instance.IsSuspend = false;
            var taskId = Guid.NewGuid().ToString("N");
            _taskId = taskId;
            _execution = ExecuteAsync(
                taskId, displayName, operation, _operationCancellation.Token);
            return new { taskId, state = "running", groupName = displayName };
        }
    }

    public object Pause(string taskId)
    {
        RequireActive(taskId);
        RunnerContext.Instance.IsSuspend = true;
        EmitAsync(taskId, "paused", null).GetAwaiter().GetResult();
        return new { taskId, state = "paused" };
    }

    public object Resume(string taskId)
    {
        RequireActive(taskId);
        RunnerContext.Instance.IsSuspend = false;
        EmitAsync(taskId, "running", null).GetAwaiter().GetResult();
        return new { taskId, state = "running" };
    }

    public object Stop(string taskId)
    {
        RequireActive(taskId);
        CancellationContext.Instance.ManualCancel();
        _operationCancellation?.Cancel();
        return new { taskId, state = "stopping" };
    }

    public async Task<bool> StopActiveAsync(CancellationToken cancellationToken)
    {
        Task? execution;
        lock (_sync)
        {
            if (_execution is not { IsCompleted: false })
                return false;
            CancellationContext.Instance.ManualCancel();
            _operationCancellation?.Cancel();
            execution = _execution;
        }

        await execution.WaitAsync(cancellationToken);
        return true;
    }

    private async Task ExecuteAsync(
        string taskId,
        string displayName,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        try
        {
            await EmitAsync(taskId, "running", null);
            await operation(cancellationToken);
            var state = CancellationContext.Instance.IsManualStop ? "cancelled" : "completed";
            await EmitAsync(taskId, state, null);
        }
        catch (OperationCanceledException) when (
            CancellationContext.Instance.IsManualStop || cancellationToken.IsCancellationRequested)
        {
            await EmitAsync(taskId, "cancelled", null);
        }
        catch (Exception ex)
        {
            await EmitAsync(taskId, "failed", new { code = ex.GetType().Name, message = ex.Message });
        }
        finally
        {
            RunnerContext.Instance.IsSuspend = false;
        }
    }

    private static async Task RunGroupsAsync(
        IReadOnlyList<ScriptGroup> groups,
        CancellationToken cancellationToken)
    {
        RunnerContext.Instance.Reset();
        RunnerContext.Instance.IsContinuousRunGroup = true;
        var taskProgress = new TaskProgress
        {
            ScriptGroupNames = groups.Select(group => group.Name).ToList(),
            Loop = false
        };
        RunnerContext.Instance.taskProgress = taskProgress;
        try
        {
            var service = new ScriptService();
            for (var index = 0; index < groups.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var group = groups[index];
                taskProgress.CurrentScriptGroupName = group.Name;
                TaskProgressManager.SaveTaskProgress(taskProgress);
                await service.RunMulti(group.Projects, group.Name, taskProgress);
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
            taskProgress.LoopCount++;
            if (taskProgress.ConsecutiveFailureCount == 0)
            {
                taskProgress.EndTime = DateTime.Now;
                TaskProgressManager.SaveTaskProgress(taskProgress);
            }
        }
        finally
        {
            RunnerContext.Instance.Reset();
        }
    }

    private static async Task RunGroupAsync(ScriptGroup group)
    {
        RunnerContext.Instance.Reset();
        var taskProgress = new TaskProgress
        {
            ScriptGroupNames = [group.Name],
            CurrentScriptGroupName = group.Name
        };
        RunnerContext.Instance.taskProgress = taskProgress;
        TaskProgressManager.SaveTaskProgress(taskProgress);
        await new ScriptService().RunMulti(group.Projects, group.Name, taskProgress);
    }

    private async Task EmitAsync(string taskId, string state, object? error)
    {
        var response = await callbacks.InvokeAsync("scheduler.event", JObject.FromObject(new
        {
            taskId,
            state,
            error
        }), sessionToken, hostCancellationToken);
        if (response?.Value<bool?>("acknowledged") != true)
            throw new InvalidDataException("scheduler.event did not return acknowledged=true.");
    }

    private void RequireActive(string taskId)
    {
        lock (_sync)
        {
            if (_execution is not { IsCompleted: false } || !string.Equals(taskId, _taskId, StringComparison.Ordinal))
                throw new InvalidOperationException($"Scheduler task '{taskId}' is not active.");
        }
    }

    private string ResolveGroup(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName) || groupName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            groupName.Contains('/') || groupName.Contains('\\') || groupName is "." or "..")
            throw new ArgumentException("Invalid script group name.", nameof(groupName));
        var path = Path.Combine(layout.ScriptGroupPath, groupName + ".json");
        if (!File.Exists(path))
            throw new FileNotFoundException($"Script group does not exist: {groupName}", path);
        return path;
    }

    private void ApplyNextProject(ScriptGroup group)
    {
        if (!File.Exists(layout.SchedulerStatePath)) return;
        try
        {
            var state = JObject.Parse(File.ReadAllText(layout.SchedulerStatePath));
            if (state.Value<string>("groupName") != group.Name) return;
            var startIndex = group.Projects.ToList().FindIndex(project =>
                project.Index == state.Value<int?>("index") &&
                project.FolderName == state.Value<string>("folderName") &&
                project.Name == state.Value<string>("projectName"));
            if (startIndex < 0) return;
            for (var index = 0; index < startIndex; index++) group.Projects[index].SkipFlag = true;
            File.Delete(layout.SchedulerStatePath);
        }
        catch (Exception exception) when (exception is IOException or Newtonsoft.Json.JsonException)
        {
            throw new InvalidDataException("The saved scheduler start position is invalid.", exception);
        }
    }
}
