using LibreHardwareMonitor.Hardware;

namespace HassLink.Sensors;

/// <summary>
/// Reads CPU and GPU temperatures (and GPU load) via LibreHardwareMonitor.
/// Requires the app to run as administrator (configured in app.manifest).
/// </summary>
public class HardwareSensor : ISensor
{
    private readonly Computer _computer;
    private readonly bool _includeCpuTemp;
    private readonly bool _includeGpuTemp;
    private bool _available;

    public string Id => "hardware";
    public string Name => "Hardware Sensors (CPU/GPU Temp)";
    public bool IsAvailable => _available;

    public HardwareSensor(bool includeCpuTemp, bool includeGpuTemp)
    {
        _includeCpuTemp = includeCpuTemp;
        _includeGpuTemp = includeGpuTemp;

        _computer = new Computer
        {
            IsCpuEnabled = includeCpuTemp,
            IsGpuEnabled = includeGpuTemp,
        };

        try
        {
            _computer.Open();
            _available = true;
        }
        catch
        {
            _available = false;
        }
    }

    public Task<IReadOnlyList<SensorReading>> GetReadingsAsync()
    {
        if (!_available)
            return Task.FromResult<IReadOnlyList<SensorReading>>([]);

        var readings = new List<SensorReading>();

        try
        {
            foreach (var hardware in _computer.Hardware)
            {
                hardware.Update();

                foreach (var subHardware in hardware.SubHardware)
                    subHardware.Update();

                if (_includeCpuTemp && hardware.HardwareType == HardwareType.Cpu)
                {
                    var tempSensors = hardware.Sensors
                        .Where(s => s.SensorType == SensorType.Temperature)
                        .ToList();

                    // Prefer "CPU Package" or "Core Average" if available
                    var primary = tempSensors.FirstOrDefault(s =>
                        s.Name.Contains("Package", StringComparison.OrdinalIgnoreCase) ||
                        s.Name.Contains("Average", StringComparison.OrdinalIgnoreCase))
                        ?? tempSensors.FirstOrDefault();

                    if (primary?.Value is float temp && temp > 0)
                        readings.Add(new("cpu_temp", "CPU Temperature", Math.Round(temp, 1).ToString("F1"), "°C", "temperature", "mdi:thermometer"));
                }

                if (_includeGpuTemp && (hardware.HardwareType == HardwareType.GpuNvidia || hardware.HardwareType == HardwareType.GpuAmd || hardware.HardwareType == HardwareType.GpuIntel))
                {
                    var gpuName = hardware.Name;
                    var safeId = MakeId(gpuName);

                    var tempSensor = hardware.Sensors
                        .FirstOrDefault(s => s.SensorType == SensorType.Temperature);
                    if (tempSensor?.Value is float gpuTemp && gpuTemp > 0)
                        readings.Add(new($"gpu_{safeId}_temp", $"GPU Temperature ({gpuName})", Math.Round(gpuTemp, 1).ToString("F1"), "°C", "temperature", "mdi:thermometer"));

                    var loadSensor = hardware.Sensors
                        .FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase));
                    if (loadSensor?.Value is float gpuLoad)
                        readings.Add(new($"gpu_{safeId}_load", $"GPU Load ({gpuName})", Math.Round(gpuLoad, 1).ToString("F1"), "%", null, "mdi:gpu"));
                }
            }
        }
        catch
        {
            // Silently skip if sensor read fails mid-cycle
        }

        return Task.FromResult<IReadOnlyList<SensorReading>>(readings);
    }

    private static string MakeId(string name)
    {
        var safe = new System.Text.StringBuilder();
        foreach (var c in name.ToLower())
            safe.Append(char.IsLetterOrDigit(c) ? c : '_');
        return safe.ToString().Trim('_');
    }

    public void Dispose()
    {
        try { _computer.Close(); }
        catch { /* ignore */ }
    }
}
