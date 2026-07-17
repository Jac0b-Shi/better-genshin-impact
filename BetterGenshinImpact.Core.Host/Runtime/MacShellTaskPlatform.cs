using BetterGenshinImpact.Core.Host;
using BetterGenshinImpact.GameTask.Shell;
using System.Diagnostics;

namespace BetterGenshinImpact.Core.Host.Runtime;

public sealed class MacShellTaskPlatform(
    Transport.PlatformCallbackChannel callbacks,
    string sessionToken,
    CancellationToken cancellationToken) : IShellTaskPlatform
{
    public void ActivateGameWindow()
    {
        var response = callbacks.InvokeAsync("window.activate", null, sessionToken, cancellationToken)
            .GetAwaiter().GetResult();
        if (response?.Value<bool?>("acknowledged") != true)
            throw new InvalidDataException("window.activate did not return acknowledged=true.");
    }

    public async Task<ShellExecutionRecord> ExecuteAsync(
        ShellTaskParam param, bool waitForExit, CancellationToken cancellationToken)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "/bin/zsh",
            RedirectStandardOutput = param.Output,
            RedirectStandardError = param.Output,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        process.StartInfo.ArgumentList.Add("-lc");
        process.StartInfo.ArgumentList.Add(param.Command);
        process.Start();
        if (!waitForExit) return new ShellExecutionRecord(false, "", "");
        var standardOutput = param.Output
            ? process.StandardOutput.ReadToEndAsync(cancellationToken)
            : Task.FromResult("");
        var standardError = param.Output
            ? process.StandardError.ReadToEndAsync(cancellationToken)
            : Task.FromResult("");
        await process.WaitForExitAsync(cancellationToken);
        var output = await standardOutput;
        var error = await standardError;
        var combined = string.Concat(output, error);
        var firstBreak = combined.IndexOf('\n');
        var shell = firstBreak >= 0 ? combined[..firstBreak] : combined;
        var remainder = firstBreak >= 0 ? combined[(firstBreak + 1)..] : "";
        return new ShellExecutionRecord(process.HasExited, shell, remainder);
    }
}
