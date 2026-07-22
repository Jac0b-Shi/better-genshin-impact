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
                if task.settingsAvailable, task.name == "AutoCook" {
                    BGIExpandableTaskCard(
                        icon: icon(for: task.name), title: task.displayName,
                        subtitle: task.unavailableReason ?? detail(for: task.name)
                    ) {
                        taskAction(task)
                    } content: {
                        autoCookSettings
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
