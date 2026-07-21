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
                BGITaskCard(icon: icon(for: task.name), title: task.displayName,
                            subtitle: task.unavailableReason ?? detail(for: task.name)) {
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
