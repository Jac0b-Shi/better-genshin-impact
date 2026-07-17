using BetterGenshinImpact.GameTask.AutoFight.Script;
using BetterGenshinImpact.Helpers;

namespace BetterGenshinImpact.Core.Runtime.Windows;

public sealed class WindowsCombatCommandPlatform : ICombatCommandPlatform
{
    public void ValidateKeyName(string keyName) => User32Helper.ToVk(keyName);
}
