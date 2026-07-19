using BetterGenshinImpact.GameTask;
using BetterGenshinImpact.GameTask.AutoPathing;
using BetterGenshinImpact.GameTask.AutoSkip;

namespace BetterGenshinImpact.Core.Runtime.Windows;

/// <summary>
/// Windows 自动剧情能力：基于现有 AutoSkipTrigger 实现，行为与原 PathExecutor 内联逻辑一致。
/// </summary>
public sealed class WindowsPathExecutorAutoSkipPlatform : IPathExecutorAutoSkipPlatform
{
    public IPathExecutorAutoSkipSession CreateSession() => new AutoSkipTriggerSession();

    private sealed class AutoSkipTriggerSession : IPathExecutorAutoSkipSession
    {
        private readonly AutoSkipTrigger _trigger = new(new AutoSkipConfig
        {
            Enabled = true,
            QuicklySkipConversationsEnabled = true, // 快速点击过剧情
            ClosePopupPagedEnabled = true,
            ClickChatOption = "优先选择最后一个选项",
        });

        public AutoSkipTriggerSession()
        {
            _trigger.Init();
        }

        public void OnCapture(CaptureContent content) => _trigger.OnCapture(content);
    }
}
