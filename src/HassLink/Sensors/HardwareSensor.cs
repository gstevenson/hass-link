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
            IsMotherboardEnabled = true, // required for AMD Ryzen SMU temperature initialisation
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
                    var reading = TryGetCpuReading(hardware);
                    if (reading is not null) readings.Add(reading);
                }

                if (_includeGpuTemp && IsGpu(hardware.HardwareType))
                    readings.AddRange(GetGpuReadings(hardware));
            }
        }
        catch
        {
            // Silently skip if sensor read fails mid-cycle
        }

        return Task.FromResult<IReadOnlyList<SensorReading>>(readings);
    }

    private static bool IsGpu(HardwareType type) =>
        type == HardwareType.GpuNvidia ||
        type == HardwareType.GpuAmd ||
        type == HardwareType.GpuIntel;

    private static SensorReading? TryGetCpuReading(IHardware hardware)
    {
        // Some CPUs (e.g. AMD Ryzen) expose temperature sensors on SubHardware
        // rather than the top-level hardware object, so search both.
        var tempSensors = hardware.Sensors
            .Concat(hardware.SubHardware.SelectMany(sh => sh.Sensors))
            .Where(s => s.SensorType == SensorType.Temperature)
            .ToList();

        // Prefer "CPU Package" or "Core Average" if available
        var primary = tempSensors.FirstOrDefault(s =>
            s.Name.Contains("Package", StringComparison.OrdinalIgnoreCase) ||
            s.Name.Contains("Average", StringComparison.OrdinalIgnoreCase))
            ?? tempSensors.FirstOrDefault();

        if (primary?.Value is not float temp || temp <= 0)
            return null;

        return new("cpu_temp", "CPU Temperature", Math.Round(temp, 1).ToString("F1"), "°C", "temperature", "mdi:thermometer");
    }

    private static List<SensorReading> GetGpuReadings(IHardware hardware)
    {
        var readings = new List<SensorReading>();
        var gpuName = hardware.Name;
        var safeId = SensorManager.SanitiseId(gpuName);

        var tempSensor = hardware.Sensors
            .FirstOrDefault(s => s.SensorType == SensorType.Temperature);
        if (tempSensor?.Value is float gpuTemp && gpuTemp > 0)
            readings.Add(new($"gpu_{safeId}_temp", $"GPU Temperature ({gpuName})", Math.Round(gpuTemp, 1).ToString("F1"), "°C", "temperature", "mdi:thermometer"));

        var loadSensor = hardware.Sensors
            .FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase));
        if (loadSensor?.Value is float gpuLoad)
            readings.Add(new($"gpu_{safeId}_load", $"GPU Load ({gpuName})", Math.Round(gpuLoad, 1).ToString("F1"), "%", null, "mdi:expansion-card"));

        return readings;
    }

    public string BuildDiagnosticReport()
    {
        var sb = new System.Text.StringBuilder();

        if (!_available)
        {
            sb.AppendLine("LibreHardwareMonitor failed to open (insufficient permissions or hardware error).");
            return sb.ToString();
        }

        foreach (var hw in _computer.Hardware)
        {
            hw.Update();
            sb.AppendLine($"[{hw.HardwareType}] {hw.Name}");

            foreach (var sensor in hw.Sensors)
                sb.AppendLine($"  {sensor.SensorType,-16} {sensor.Name,-36} {FormatValue(sensor)}");

            foreach (var sub in hw.SubHardware)
            {
                sub.Update();
                sb.AppendLine($"  SubHardware: [{sub.HardwareType}] {sub.Name}");
                foreach (var sensor in sub.Sensors)
                    sb.AppendLine($"    {sensor.SensorType,-16} {sensor.Name,-34} {FormatValue(sensor)}");
            }

            sb.AppendLine();
        }

        if (!_computer.Hardware.Any())
            sb.AppendLine("No hardware detected.");

        return sb.ToString();

        static string FormatValue(LibreHardwareMonitor.Hardware.ISensor s) =>
            s.Value.HasValue ? $"{s.Value.Value:F1} {s.SensorType switch {
                SensorType.Temperature => "°C",
                SensorType.Load or SensorType.Level => "%",
                SensorType.Fan => "RPM",
                SensorType.Power => "W",
                SensorType.Voltage => "V",
                SensorType.Clock => "MHz",
                _ => ""
            }}" : "null";
    }

    public void Dispose()
    {
        try { _computer.Close(); }
        catch { /* ignore */ }
    }
}
