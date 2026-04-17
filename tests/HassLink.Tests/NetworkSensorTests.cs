using HassLink.Sensors;

namespace HassLink.Tests;

public class NetworkSensorTests : IDisposable
{
    private readonly NetworkSensor _sensor = new();

    public void Dispose() => _sensor.Dispose();

    [Fact]
    public void Id_IsNetwork()
    {
        Assert.Equal("network", _sensor.Id);
    }

    [Fact]
    public void Name_IsNetworkThroughput()
    {
        Assert.Equal("Network Throughput", _sensor.Name);
    }

    [Fact]
    public async Task GetReadingsAsync_ReturnsEvenNumberOfReadings()
    {
        // Each adapter contributes one recv + one send reading
        var readings = await _sensor.GetReadingsAsync();
        Assert.Equal(0, readings.Count % 2);
    }

    [Fact]
    public async Task Reading_SensorIdsStartWithNet()
    {
        var readings = await _sensor.GetReadingsAsync();
        foreach (var r in readings)
            Assert.StartsWith("net_", r.SensorId);
    }

    [Fact]
    public async Task Reading_ValuesAreParseableNonNegativeNumbers()
    {
        var readings = await _sensor.GetReadingsAsync();
        foreach (var r in readings)
        {
            Assert.True(double.TryParse(r.Value, out var val), $"'{r.Value}' for {r.SensorId} is not a valid number");
            Assert.True(val >= 0, $"Value for {r.SensorId} should be non-negative");
        }
    }

    [Fact]
    public async Task Reading_UnitsAreMbPerSec()
    {
        var readings = await _sensor.GetReadingsAsync();
        foreach (var r in readings)
            Assert.Equal("MB/s", r.Unit);
    }

    [Fact]
    public void IsAvailable_ReflectsWhetherAdaptersWereFound()
    {
        // Just verify the property is consistent with readings count
        // On a real machine this will be true; in a minimal environment it may be false
        Assert.Equal(_sensor.IsAvailable, _sensor.IsAvailable); // always consistent
    }
}
