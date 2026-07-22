import SwiftUI

struct FeaturesPage: View {
    @EnvironmentObject private var appState: AppState

    var body: some View {
        VStack(alignment: .leading, spacing: 14) {
            BGIPageTitle(title: "实时触发的自动化任务设置")
            ForEach(appState.features) { feature in
                BGITaskCard(icon: feature.icon, title: feature.name, subtitle: feature.detail) {
                    Toggle(
                        "",
                        isOn: Binding(
                            get: { appState.featureEnabled(feature.id) },
                            set: { appState.setFeature(feature.id, enabled: $0) }
                        )
                    )
                    .toggleStyle(.switch)
                    .labelsHidden()
                    .disabled(!appState.canControlFeature(feature.id))
                }
            }
        }
    }
}

struct SoloTasksPage: View {
    @EnvironmentObject private var appState: AppState

    var body: some View {
        VStack(alignment: .leading, spacing: 14) {
            BGIPageTitle(title: "独立任务设置")
            ForEach(appState.soloTasks) { task in
                if task.settingsAvailable {
                    BGIExpandableTaskCard(
                        icon: icon(for: task.name), title: task.displayName,
                        subtitle: task.unavailableReason ?? detail(for: task.name)
                    ) {
                        taskAction(task)
                    } content: {
                        settingsContent(for: task.name)
                    }
                } else {
                    BGITaskCard(icon: icon(for: task.name), title: task.displayName,
                                subtitle: task.unavailableReason ?? detail(for: task.name)) {
                        taskAction(task)
                    }
                }
            }
        }
    }

    @ViewBuilder
    private func settingsContent(for name: String) -> some View {
        switch name {
        case "AutoCook": autoCookSettings
        case "AutoWood": autoWoodSettings
        case "AutoMusicGame": autoMusicGameSettings
        case "AutoBoss": autoBossSettings
        default:
            BGISettingLine(title: "设置", subtitle: "Core 未返回该任务的设置模型") {
                BGIStatusBadge(text: "不可用", tint: BGIColors.muted)
            }
        }
    }

    @ViewBuilder
    private func taskAction(_ task: BetterGICoreSoloTask) -> some View {
        if task.available {
            Button {
                appState.toggleSoloTask(task.name)
            } label: {
                Image(systemName: isRunning(task.name) ? "stop.fill" : "play.fill")
            }
            .buttonStyle(.borderedProminent)
            .disabled(appState.soloTaskStatus.state == "stopping")
            .help(isRunning(task.name) ? "停止" : "启动")
        } else {
            Text("Core 暂未开放")
                .font(.caption)
                .foregroundStyle(.secondary)
        }
    }

    @ViewBuilder
    private var autoCookSettings: some View {
        if let settings = appState.autoCookSettings {
            BGISettingLine(title: "检测间隔（毫秒）", subtitle: "每次截图检测的时间间隔，最小 1ms") {
                Stepper(
                    value: Binding(
                        get: { settings.checkIntervalMs },
                        set: { appState.saveAutoCookSettings(checkIntervalMs: $0) }
                    ),
                    in: 1...1000
                ) {
                    Text("\(settings.checkIntervalMs)")
                        .frame(minWidth: 42, alignment: .trailing)
                }
            }
            BGISettingLine(
                title: "自动结束烹饪任务",
                subtitle: "开启后检测到“自动烹饪”按钮会点击并结束当前任务"
            ) {
                Toggle(
                    "",
                    isOn: Binding(
                        get: { settings.stopTaskWhenRecoverButtonDetected },
                        set: { appState.saveAutoCookSettings(stopWhenDetected: $0) }
                    )
                )
                .toggleStyle(.switch)
                .labelsHidden()
            }
        } else {
            BGISettingLine(title: "设置", subtitle: "正在从 BetterGI C# Core 读取") {
                ProgressView().controlSize(.small)
            }
        }
    }

    @ViewBuilder
    private var autoWoodSettings: some View {
        if let settings = appState.autoWoodSettings {
            BGISettingLine(title: "使用进出千星奇域刷新树木CD", subtitle: "需要确保已解锁并进入过千星奇域") {
                Toggle("", isOn: Binding(
                    get: { settings.useWonderlandRefresh },
                    set: { appState.saveAutoWoodSettings(useWonderlandRefresh: $0) }))
                    .toggleStyle(.switch).labelsHidden()
            }
            BGISettingLine(title: "循环次数", subtitle: "输入 0 则为无限循环直到手动终止") {
                TextField("", value: Binding(
                    get: { settings.roundNum },
                    set: { appState.saveAutoWoodSettings(roundNum: $0) }), format: .number)
                    .frame(width: 90).multilineTextAlignment(.trailing)
            }
            BGISettingLine(title: "启用OCR伐木数量限制（需1080P以上分辨率）", subtitle: "达到上限后自动停止伐木") {
                Toggle("", isOn: Binding(
                    get: { settings.woodCountOcrEnabled },
                    set: { appState.saveAutoWoodSettings(woodCountOcrEnabled: $0) }))
                    .toggleStyle(.switch).labelsHidden()
            }
            BGISettingLine(title: "伐木数量上限", subtitle: "原神每日每种木材最多2000，输入 0 等同不设上限") {
                TextField("", value: Binding(
                    get: { settings.dailyMaxCount },
                    set: { appState.saveAutoWoodSettings(dailyMaxCount: $0) }), format: .number)
                    .frame(width: 90).multilineTextAlignment(.trailing)
            }
            BGISettingLine(title: "使用小道具后的额外延迟（毫秒）", subtitle: "用于观察使用小道具后获得木材的提示") {
                TextField("", value: Binding(
                    get: { settings.afterZSleepDelay },
                    set: { appState.saveAutoWoodSettings(afterZSleepDelay: $0) }), format: .number)
                    .frame(width: 90).multilineTextAlignment(.trailing)
            }
        } else { settingsLoading }
    }

    @ViewBuilder
    private var autoMusicGameSettings: some View {
        if let settings = appState.autoMusicGameSettings {
            BGISettingLine(
                title: "【专辑】 自动演奏未达成【大音天籁】的乐曲",
                subtitle: "关闭时奖励已领取就跳过；开启时达成大音天籁才跳过"
            ) {
                Toggle("", isOn: Binding(
                    get: { settings.mustCanorusLevel },
                    set: { appState.saveAutoMusicGameSettings(mustCanorusLevel: $0) }))
                    .toggleStyle(.switch).labelsHidden()
            }
            BGISettingLine(title: "【专辑】 自动演奏的目标难度", subtitle: "传说或大师可获取全部奖励；所有会演奏全部难度") {
                Picker("", selection: Binding(
                    get: { settings.musicLevel },
                    set: { appState.saveAutoMusicGameSettings(musicLevel: $0) })) {
                    ForEach(settings.musicLevelOptions, id: \.self) { Text($0).tag($0) }
                }
                .labelsHidden().frame(width: 120)
            }
        } else { settingsLoading }
    }

    @ViewBuilder
    private var autoBossSettings: some View {
        if let settings = appState.autoBossSettings {
            BGISettingLine(title: "选择战斗策略", subtitle: "仅用于首领讨伐，不覆盖其他策略设置") {
                Picker("", selection: Binding(
                    get: { settings.strategyName },
                    set: { appState.saveAutoBossSettings(strategyName: $0) })) {
                    ForEach(settings.strategyOptions, id: \.self) { Text($0).tag($0) }
                }.labelsHidden().frame(width: 220)
            }
            BGISettingLine(title: "选择首领", subtitle: "部分首领因机制问题未添加") {
                Picker("", selection: Binding(
                    get: { settings.bossName },
                    set: { appState.saveAutoBossSettings(bossName: $0) })) {
                    Text("未选择").tag("")
                    ForEach(settings.bossOptions, id: \.self) { Text($0).tag($0) }
                }.labelsHidden().frame(width: 220)
            }
            BGISettingLine(title: "切换队伍", subtitle: "留空则不更换队伍") {
                TextField("例如：首领队", text: Binding(
                    get: { settings.teamName },
                    set: { appState.saveAutoBossSettings(teamName: $0) }))
                    .frame(width: 200)
            }
            BGISettingLine(title: "指定讨伐次数", subtitle: "关闭时刷取至原粹树脂耗尽") {
                Toggle("", isOn: Binding(
                    get: { settings.specifyRunCount },
                    set: { appState.saveAutoBossSettings(specifyRunCount: $0) }))
                    .toggleStyle(.switch).labelsHidden()
            }
            if settings.specifyRunCount {
                BGISettingLine(title: "讨伐次数", subtitle: "按成功领取奖励次数停止") {
                    Stepper(value: Binding(
                        get: { settings.runCount },
                        set: { appState.saveAutoBossSettings(runCount: $0) }), in: 1...999) {
                        Text("\(settings.runCount)").frame(minWidth: 36, alignment: .trailing)
                    }
                }
                BGISettingLine(title: "原粹不足时使用须臾树脂补充", subtitle: "仅在指定讨伐次数时生效") {
                    Toggle("", isOn: Binding(
                        get: { settings.useTransientResin },
                        set: { appState.saveAutoBossSettings(useTransientResin: $0) }))
                        .toggleStyle(.switch).labelsHidden()
                }
                BGISettingLine(title: "原粹不足时使用脆弱树脂补充", subtitle: "仅在指定讨伐次数时生效") {
                    Toggle("", isOn: Binding(
                        get: { settings.useFragileResin },
                        set: { appState.saveAutoBossSettings(useFragileResin: $0) }))
                        .toggleStyle(.switch).labelsHidden()
                }
            }
            BGISettingLine(title: "每轮讨伐后返回七天神像", subtitle: "每次领奖后先回血，再重新前往首领") {
                Toggle("", isOn: Binding(
                    get: { settings.returnToStatueAfterEachRound },
                    set: { appState.saveAutoBossSettings(returnToStatueAfterEachRound: $0) }))
                    .toggleStyle(.switch).labelsHidden()
            }
            BGISettingLine(title: "启用奖励识别", subtitle: "每轮领取后识别奖励名称与数量，任务结束打印汇总") {
                Toggle("", isOn: Binding(
                    get: { settings.rewardRecognitionEnabled },
                    set: { appState.saveAutoBossSettings(rewardRecognitionEnabled: $0) }))
                    .toggleStyle(.switch).labelsHidden()
            }
            BGISettingLine(title: "角色死亡后重试次数", subtitle: "复活后重新讨伐当前首领") {
                Stepper(value: Binding(
                    get: { settings.reviveRetryCount },
                    set: { appState.saveAutoBossSettings(reviveRetryCount: $0) }), in: 0...99) {
                    Text("\(settings.reviveRetryCount)").frame(minWidth: 30, alignment: .trailing)
                }
            }
        } else { settingsLoading }
    }

    private var settingsLoading: some View {
        BGISettingLine(title: "设置", subtitle: "正在从 BetterGI C# Core 读取") {
            ProgressView().controlSize(.small)
        }
    }

    private func isRunning(_ name: String) -> Bool {
        appState.soloTaskStatus.name == name &&
            ["running", "stopping"].contains(appState.soloTaskStatus.state)
    }

    private func icon(for name: String) -> BGIIcon {
        name == "AutoFishing" ? .fgi("\u{e3a8}") : .symbol("gearshape.2")
    }

    private func detail(for name: String) -> String {
        name == "AutoFishing"
            ? "在出现钓鱼交互提示的位置启动；识别、抛竿和收杆均由共享 C# 任务执行。"
            : "BetterGI C# Core 独立任务。"
    }
}
