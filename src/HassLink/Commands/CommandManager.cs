using System.Diagnostics;
using System.Runtime.InteropServices;
using HassLink.Config;
using HassLink.Mqtt;
using HassLink.Sensors;

namespace HassLink.Commands;

public class CommandManager : IDisposable
{
    private readonly MqttService _mqtt;
    private AppConfig _config;

    public CommandManager(MqttService mqtt, AppConfig config)
    {
        _mqtt = mqtt;
        _config = config;
        _mqtt.MessageReceived += OnMessageReceived;
    }

    public async Task StartAsync()
    {
        var deviceId = SensorManager.SanitiseId(_config.DeviceName);
        foreach (var (id, cmd) in _config.Commands)
        {
            if (!cmd.Enabled) continue;
            await _mqtt.SubscribeAsync(CommandTopic(deviceId, id));
        }
    }

    public async Task RestartAsync(AppConfig config)
    {
        await StopAsync();
        _config = config;
        await StartAsync();
    }

    public async Task StopAsync()
    {
        var deviceId = SensorManager.SanitiseId(_config.DeviceName);
        foreach (var (id, _) in _config.Commands)
            await _mqtt.UnsubscribeAsync(CommandTopic(deviceId, id));
    }

    private void OnMessageReceived(string topic, string payload)
    {
        if (string.IsNullOrWhiteSpace(payload)) return;

        var deviceId = SensorManager.SanitiseId(_config.DeviceName);
        foreach (var (id, cmd) in _config.Commands)
        {
            if (!cmd.Enabled) continue;
            if (topic != CommandTopic(deviceId, id)) continue;
            ExecuteCommand(cmd);
            break;
        }
    }

    private string CommandTopic(string deviceId, string commandId) =>
        $"{_config.Mqtt.BaseTopic}/{deviceId}/{commandId}/set";

    private static void ExecuteCommand(CommandConfig cmd)
    {
        try
        {
            switch (cmd.Type)
            {
                case "shutdown":
                    Process.Start(new ProcessStartInfo("shutdown.exe", "/s /t 0") { CreateNoWindow = true });
                    break;
                case "restart":
                    Process.Start(new ProcessStartInfo("shutdown.exe", "/r /t 0") { CreateNoWindow = true });
                    break;
                case "sleep":
                    Application.SetSuspendState(PowerState.Suspend, false, false);
                    break;
                case "hibernate":
                    Application.SetSuspendState(PowerState.Hibernate, false, false);
                    break;
                case "lock":
                    LockWorkStation();
                    break;
                case "custom":
                    if (!string.IsNullOrEmpty(cmd.Executable))
                        Process.Start(new ProcessStartInfo(cmd.Executable, cmd.Arguments ?? "")
                        {
                            UseShellExecute = true,
                        });
                    break;
            }
        }
        catch { }
    }

    [DllImport("user32.dll")]
    private static extern bool LockWorkStation();

    public void Dispose()
    {
        _mqtt.MessageReceived -= OnMessageReceived;
    }
}
