using HassLink.Config;
using HassLink.Mqtt;

namespace HassLink.Sensors;

/// <summary>
/// Owns all sensor instances, drives a polling timer, and publishes
/// readings to MQTT via MqttService.
/// </summary>
public class SensorManager : IDisposable
{
    private readonly IMqttPublisher _mqtt;
    private readonly List<ISensor> _sensors = [];
    private System.Windows.Forms.Timer? _timer;
    private AppConfig _config;

    public SensorManager(IMqttPublisher mqtt, AppConfig config)
    {
        _mqtt = mqtt;
        _config = config;
    }

    public void Start(AppConfig config)
    {
        _config = config;
        RebuildSensors();
        StartTimer();
    }

    public void Restart(AppConfig config)
    {
        _config = config;
        StopTimer();
        DisposeAllSensors();
        RebuildSensors();
        StartTimer();
    }

    private void RebuildSensors()
    {
        DisposeAllSensors();

        if (_config.GetSensor("cpu").Enabled)
            _sensors.Add(new CpuSensor());

        if (_config.GetSensor("ram").Enabled)
            _sensors.Add(new RamSensor());

        if (_config.GetSensor("disk").Enabled)
            _sensors.Add(new DiskSensor());

        if (_config.GetSensor("network").Enabled)
            _sensors.Add(new NetworkSensor());

        if (_config.GetSensor("activeWindow").Enabled)
            _sensors.Add(new ActiveWindowSensor());

        if (_config.GetSensor("uptime").Enabled)
            _sensors.Add(new UptimeSensor());

        if (_config.GetSensor("battery").Enabled)
        {
            var bat = new BatterySensor();
            if (bat.IsAvailable) _sensors.Add(bat);
            else bat.Dispose();
        }

        var wantCpuTemp = _config.GetSensor("cpuTemp").Enabled;
        var wantGpuTemp = _config.GetSensor("gpuTemp").Enabled;
        if (wantCpuTemp || wantGpuTemp)
            _sensors.Add(new HardwareSensor(wantCpuTemp, wantGpuTemp));
    }

    private void StartTimer()
    {
        var intervalMs = Math.Max(5, _config.PublishIntervalSeconds) * 1000;
        _timer = new System.Windows.Forms.Timer { Interval = intervalMs };
        _timer.Tick += async (_, _) => await PublishAllAsync();
        _timer.Start();
    }

    private void StopTimer()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }

    private async Task PublishAllAsync()
    {
        if (!_mqtt.IsConnected) return;

        var deviceId = SanitiseId(_config.DeviceName);

        foreach (var sensor in _sensors)
        {
            try
            {
                var readings = await sensor.GetReadingsAsync();
                foreach (var reading in readings)
                {
                    var topic = $"{_config.Mqtt.BaseTopic}/{deviceId}/{reading.SensorId}/state";
                    await _mqtt.PublishAsync(topic, reading.Value, retain: true);
                }
            }
            catch
            {
                // Sensor read failure is non-fatal — skip and continue
            }
        }
    }

    /// <summary>Returns all sensors for use by HA discovery and settings UI.</summary>
    public IReadOnlyList<ISensor> GetSensors() => _sensors.AsReadOnly();

    public string GetHardwareDiagnostics()
    {
        var hw = _sensors.OfType<HardwareSensor>().FirstOrDefault();
        return hw?.BuildDiagnosticReport() ?? "Hardware sensor is not enabled.";
    }

    public static string SanitiseId(string name)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var c in name.ToLower())
            sb.Append(char.IsLetterOrDigit(c) ? c : '_');
        return sb.ToString().Trim('_');
    }

    private void DisposeAllSensors()
    {
        foreach (var s in _sensors) s.Dispose();
        _sensors.Clear();
    }

    public void Dispose()
    {
        StopTimer();
        DisposeAllSensors();
    }
}
