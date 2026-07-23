using BetterGenshinImpact.Core.Host;
using BetterGenshinImpact.Core.Host.Protocol;
using BetterGenshinImpact.Core.Host.Runtime;
using BetterGenshinImpact.Core.Host.Transport;
using BetterGenshinImpact.Core.Recorder;
using BetterGenshinImpact.Core.Recorder.Model;
using BetterGenshinImpact.Verification.Framework;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Net.Sockets;
using System.Runtime.Versioning;

namespace BetterGenshinImpact.Core.Host.Fast.Verification;

public sealed class RuntimeSettingsSuite : IVerificationSuite
{
    public string Name => "runtime-settings";

    [SupportedOSPlatform("macos")]
    public async Task RunAsync(VerificationContext context, CancellationToken cancellationToken)
    {
        var root = Path.Combine(Path.GetTempPath(), $"bettergi-runtime-settings-{Guid.NewGuid():N}");
        try
        {
            var layout = new RuntimeLayout(root);
            layout.EnsureCreated();
            var configPath = Path.Combine(layout.UserPath, "config.json");
            await File.WriteAllTextAsync(configPath, """
                {
                  "preserved": { "value": 42 },
                  "macroConfig": {
                    "fPressHoldToContinuationEnabled": false,
                    "fFireInterval": 100,
                    "spacePressHoldToContinuationEnabled": false,
                    "spaceFireInterval": 100
                  },
                  "keyBindingsConfig": {
                    "moveForward": 71,
                    "normalAttack": 5,
                    "pickUpOrInteract": 71,
                    "jump": 74
                  },
                  "notificationConfig": {
                    "jsNotificationEnabled": false,
                    "windowsUwpNotificationEnabled": false
                  }
                }
                """, cancellationToken);

            var macroCatalog = new MacroSettingsCatalog(layout);
            var keyResolver = new GameActionKeyResolver(layout);
            var moveForward = keyResolver.Resolve(
                BetterGenshinImpact.Core.Simulator.Extensions.GIActions.MoveForward);
            var normalAttack = keyResolver.Resolve(
                BetterGenshinImpact.Core.Simulator.Extensions.GIActions.NormalAttack);
            var openMap = keyResolver.Resolve(
                BetterGenshinImpact.Core.Simulator.Extensions.GIActions.OpenMap);
            context.Require(
                moveForward.WindowsVirtualKey == 71 &&
                moveForward.MouseButton is null &&
                normalAttack.WindowsVirtualKey is null &&
                normalAttack.MouseButton == "side1" &&
                openMap.WindowsVirtualKey == 0x4D,
                "GameActionKeyResolver did not preserve configured keys, mouse buttons or upstream defaults.");

            _ = macroCatalog.Save(JObject.FromObject(new
            {
                fPressHoldToContinuationEnabled = true,
                fFireInterval = 80,
                spacePressHoldToContinuationEnabled = true,
                spaceFireInterval = 120,
            }));

            using var loggerFactory = LoggerFactory.Create(_ => { });
            var scriptHostServices = new MacScriptHostServices(loggerFactory);
            var notificationCatalog = new NotificationSettingsCatalog(
                layout, new PlatformCallbackChannel(), "verification", cancellationToken);
            notificationCatalog.AttachScriptHostServices(scriptHostServices);
            _ = notificationCatalog.Save(JObject.FromObject(new
            {
                jsNotificationEnabled = true,
                macOSNotificationEnabled = true,
            }));

            var persisted = JObject.Parse(await File.ReadAllTextAsync(configPath, cancellationToken));
            context.Require(
                persisted["preserved"]?.Value<int>("value") == 42 &&
                persisted["macroConfig"]?.Value<int>("fFireInterval") == 80 &&
                persisted["macroConfig"]?.Value<int>("spaceFireInterval") == 120 &&
                persisted["notificationConfig"]?.Value<bool>("jsNotificationEnabled") == true &&
                persisted["notificationConfig"]?.Value<bool>("windowsUwpNotificationEnabled") == true &&
                scriptHostServices.JsNotificationEnabled,
                "Runtime settings did not preserve unrelated config or update Core notification state.");

            var script = KeyMouseScriptBuilder.Build(
            [
                new MacroEvent { Type = MacroEventType.MouseMoveBy, MouseX = 2, MouseY = 3, Time = 10 },
                new MacroEvent { Type = MacroEventType.MouseMoveBy, MouseX = 4, MouseY = 5, Time = 25 },
                new MacroEvent { Type = MacroEventType.KeyDown, KeyCode = 70, Time = 30 },
                new MacroEvent { Type = MacroEventType.KeyUp, KeyCode = 70, Time = 40 },
                new MacroEvent { Type = MacroEventType.MouseMoveTo, MouseX = 90, MouseY = 80, Time = -1 },
            ],
            new KeyMouseScriptInfo { Width = 1920, Height = 1080, RecordDpi = 2 });
            context.Require(
                script.MacroEvents.Count == 3 &&
                script.MacroEvents[0].Type == MacroEventType.MouseMoveBy &&
                script.MacroEvents[0].MouseX == 6 &&
                script.MacroEvents[0].MouseY == 8 &&
                script.MacroEvents.All(macroEvent => macroEvent.Time >= 0),
                "KeyMouseScriptBuilder drifted from upstream ordering, filtering or merge semantics.");

            await VerifyRpcSurfaceAsync(
                context, layout, loggerFactory, scriptHostServices,
                notificationCatalog, cancellationToken);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [SupportedOSPlatform("macos")]
    private static async Task VerifyRpcSurfaceAsync(
        VerificationContext context,
        RuntimeLayout layout,
        ILoggerFactory loggerFactory,
        MacScriptHostServices scriptHostServices,
        NotificationSettingsCatalog notificationCatalog,
        CancellationToken cancellationToken)
    {
        var socketPath = Path.Combine(
            "/tmp", $"bgi-rpc-{Guid.NewGuid():N}"[..24] + ".sock");
        const string sessionToken = "runtime-settings-verification";
        using var serverCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var server = new CoreRpcServer(
            socketPath,
            sessionToken,
            layout,
            new NativeDependencyStatus("8.0", "Arm64", "4.13.0", true));
        server.AttachScriptHostServices(scriptHostServices);
        server.AttachNotificationSettings(notificationCatalog);
        server.AttachKeyMouseScriptCoordinator(new KeyMouseScriptCoordinator(
            layout,
            loggerFactory.CreateLogger<KeyMouseScriptCoordinator>(),
            serverCancellation.Token));
        var serverTask = server.RunAsync(serverCancellation.Token);

        try
        {
            for (var retry = 0; retry < 100 && !File.Exists(socketPath); retry++)
                await Task.Delay(10, cancellationToken);
            context.Require(File.Exists(socketPath), "Core RPC socket was not created.");

            using var socket = new Socket(
                AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            await socket.ConnectAsync(new UnixDomainSocketEndPoint(socketPath), cancellationToken);
            await using var connection = new FramedJsonConnection(socket);

            var macroSave = await ExchangeAsync(
                connection, "macro-save", "macro.settings.save", sessionToken,
                JObject.FromObject(new
                {
                    settings = new
                    {
                        fPressHoldToContinuationEnabled = true,
                        fFireInterval = 70,
                        spacePressHoldToContinuationEnabled = false,
                        spaceFireInterval = 130,
                    }
                }), cancellationToken);
            var macroResult = JObject.FromObject(macroSave.Result!);
            context.Require(
                macroSave.Error is null &&
                macroResult.Value<int>("fFireInterval") == 70 &&
                macroResult.Value<int>("spaceFireInterval") == 130 &&
                macroResult.Value<int>("pickUpOrInteractKeyCode") == 71 &&
                macroResult.Value<int>("jumpKeyCode") == 74,
                macroSave.Error?.Message ?? "macro.settings.save returned an invalid result.");

            var notificationGet = await ExchangeAsync(
                connection, "notification-get", "notification.settings.get",
                sessionToken, null, cancellationToken);
            var notificationResult = JObject.FromObject(notificationGet.Result!);
            context.Require(
                notificationGet.Error is null &&
                notificationResult.Value<bool>("jsNotificationEnabled") &&
                notificationResult.Value<bool>("macOSNotificationEnabled"),
                notificationGet.Error?.Message ?? "notification.settings.get returned an invalid result.");

            var saveRecording = await ExchangeAsync(
                connection, "recording-save", "keyMouse.saveRecording", sessionToken,
                JObject.FromObject(new
                {
                    events = new object[]
                    {
                        new { type = 0, keyCode = 70, mouseX = 0, mouseY = 0, time = 10 },
                        new { type = 1, keyCode = 70, mouseX = 0, mouseY = 0, time = 40 },
                    },
                    info = new { x = 0, y = 0, width = 1920, height = 1080, recordDpi = 2 },
                }), cancellationToken);
            var saved = JObject.FromObject(saveRecording.Result!);
            var scriptId = saved.Value<string>("id");
            context.Require(
                saveRecording.Error is null && !string.IsNullOrWhiteSpace(scriptId) &&
                saved.Value<int>("eventCount") == 2,
                saveRecording.Error?.Message ?? "keyMouse.saveRecording returned an invalid result.");

            var list = await ExchangeAsync(
                connection, "recording-list", "keyMouse.list", sessionToken,
                null, cancellationToken);
            var listed = JArray.FromObject(list.Result!);
            context.Require(
                list.Error is null &&
                listed.Any(item => item.Value<string>("id") == scriptId),
                list.Error?.Message ?? "keyMouse.list did not return the saved recording.");

            var rename = await ExchangeAsync(
                connection, "recording-rename", "keyMouse.rename", sessionToken,
                JObject.FromObject(new { id = scriptId, name = "RPC 验证" }),
                cancellationToken);
            var renamed = JObject.FromObject(rename.Result!);
            var renamedId = renamed.Value<string>("id");
            context.Require(
                rename.Error is null && renamedId == "RPC 验证.json",
                rename.Error?.Message ?? "keyMouse.rename returned an invalid result.");

            var delete = await ExchangeAsync(
                connection, "recording-delete", "keyMouse.delete", sessionToken,
                JObject.FromObject(new { id = renamedId }), cancellationToken);
            var deleted = JObject.FromObject(delete.Result!);
            context.Require(
                delete.Error is null && deleted.Value<bool>("deleted"),
                delete.Error?.Message ?? "keyMouse.delete returned an invalid result.");
        }
        finally
        {
            serverCancellation.Cancel();
            await serverTask;
        }
    }

    private static async Task<RpcResponse> ExchangeAsync(
        FramedJsonConnection connection,
        string id,
        string method,
        string sessionToken,
        JObject? parameters,
        CancellationToken cancellationToken)
    {
        await connection.WriteRequestAsync(
            new RpcRequest(id, method, parameters, sessionToken), cancellationToken);
        return await connection.ReadResponseAsync(cancellationToken)
            ?? throw new EndOfStreamException($"Core disconnected during {method}.");
    }
}
