namespace HassLink.Sensors;

public class UptimeSensor : ISensor
{
    public string Id => "uptime";
    public string Name => "System Uptime";
    public bool IsAvailable => true;

    public Task<IReadOnlyList<SensorReading>> GetReadingsAsync()
    {
        var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
        var hours = Math.Round(uptime.TotalHours, 2);

        IReadOnlyList<SensorReading> readings =
        [
            new("uptime_hours", "System Uptime", hours.ToString("F2"), "h", "duration", "mdi:timer-outline")
        ];
        return Task.FromResult(readings);
    }

    public void Dispose() { }
}
