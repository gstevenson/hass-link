using System.Diagnostics;

namespace HassLink.Sensors;

public class CpuSensor : ISensor
{
    private readonly PerformanceCounter _counter;
    private bool _primed;

    public string Id => "cpu";
    public string Name => "CPU Usage";
    public bool IsAvailable => true;

    public CpuSensor()
    {
        _counter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
    }

    public Task<IReadOnlyList<SensorReading>> GetReadingsAsync()
    {
        // First call always returns 0 — prime on first real call
        if (!_primed)
        {
            _counter.NextValue();
            _primed = true;
        }

        var value = Math.Round(_counter.NextValue(), 1);
        IReadOnlyList<SensorReading> readings =
        [
            new("cpu", "CPU Usage", value.ToString("F1"), "%", null, "mdi:cpu-64-bit")
        ];
        return Task.FromResult(readings);
    }

    public void Dispose() => _counter.Dispose();
}
