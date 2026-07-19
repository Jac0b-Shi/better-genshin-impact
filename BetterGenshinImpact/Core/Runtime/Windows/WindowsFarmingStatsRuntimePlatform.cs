using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.GameTask;
using BetterGenshinImpact.GameTask.FarmingPlan;
using BetterGenshinImpact.GameTask.LogParse;
using BetterGenshinImpact.Helpers;
using Microsoft.Extensions.Logging;

namespace BetterGenshinImpact.Core.Runtime.Windows;

public sealed class WindowsFarmingStatsRuntimePlatform : IFarmingStatsRuntimePlatform
{
    public string LogDirectory => Global.Absolute(@"log\FarmingPlan");
    public OtherConfig.FarmingPlan Config => TaskContext.Instance().Config.OtherConfig.FarmingPlanConfig;
    public ILogger Logger => App.GetLogger<WindowsFarmingStatsRuntimePlatform>();
    public DateTimeOffset ServerTimeNow => ServerTimeHelper.GetServerTimeNow();

    public async Task UpdateMiyousheDataAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var other = TaskContext.Instance().Config.OtherConfig;
        DailyFarmingData? dailyFarmingData = null;
        if (other.FarmingPlanConfig.MiyousheDataConfig.Enabled &&
            !string.IsNullOrEmpty(other.MiyousheConfig.Cookie))
        {
            try
            {
                var gameInfo = await TravelsDiaryDetailManager.UpdateTravelsDiaryDetailManager(
                    other.MiyousheConfig.Cookie, true);
                cancellationToken.ThrowIfCancellationRequested();
                var actionItems = TravelsDiaryDetailManager.loadNowDayActionItems(gameInfo);
                var statistics = new MoraStatistics();
                statistics.ActionItems.AddRange(actionItems);
                dailyFarmingData = FarmingStatsRecorder.ReadDailyFarmingData();
                if (actionItems.Count > 0)
                {
                    dailyFarmingData.MiyousheTotalEliteMobCount = statistics.EliteGameStatistics;
                    dailyFarmingData.MiyousheTotalNormalMobCount = statistics.SmallMonsterStatistics;
                    dailyFarmingData.TravelsDiaryDetailManagerUpdateTime = DateTime.Parse(actionItems.Last().Time);
                    FarmingStatsRecorder.debugInfo(
                        $"札记当天数据：[精英：{dailyFarmingData.MiyousheTotalEliteMobCount},小怪：{dailyFarmingData.MiyousheTotalNormalMobCount},{dailyFarmingData.TravelsDiaryDetailManagerUpdateTime}]");
                }
                else
                {
                    Logger.LogError("米游社旅行札记未有数据！");
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "米游社数据更新失败，请检查cookie是否过期");
            }
        }

        dailyFarmingData ??= FarmingStatsRecorder.ReadDailyFarmingData();
        dailyFarmingData.LastMiyousheUpdateTime = DateTime.Now;
        FarmingStatsRecorder.SaveDailyData(dailyFarmingData.FilePath, dailyFarmingData);
    }
}
