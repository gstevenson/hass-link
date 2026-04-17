using HassLink.Sensors;

namespace HassLink.Tests;

public class CpuSensorTests : IDisposable
{
    private readonly CpuSensor _sensor = new();

    public void Dispose() => _sensor.Dispose();

    [Fact]
    public void Id_IsCpu()
    {
        Assert.Equal("cpu", _sensor.Id);
    }

    [Fact]
    public void Name_IsCpuUsage()
    {
        Assert.Equal("CPU Usage", _sensor.Name);
    }

    [Fact]
    public void IsAvailable_IsTrue()
    {
        Assert.True(_sensor.IsAvailable);
    }

    [Fact]
    public async Task GetReadingsAsync_ReturnsExactlyOneReading()
    {
        var readings = await _sensor.GetReadingsAsync();
        Assert.Single(readings);
    }

    [Fact]
    public async Task Reading_HasExpectedSensorId()
    {
        var readings = await _sensor.GetReadingsAsync();
        Assert.Equal("cpu", readings[0].SensorId);
    }

    [Fact]
    public async Task Reading_HasPercentUnit()
    {
        var readings = await _sensor.GetReadingsAsync();
        Assert.Equal("%", readings[0].Unit);
    }

    [Fact]
    public async Task Reading_ValueIsParseableNumber()
    {
        var readings = await _sensor.GetReadingsAsync();
        Assert.True(double.TryParse(readings[0].Value, out _), $"'{readings[0].Value}' is not a valid number");
    }

    [Fact]
    public async Task Reading_ValueIsInValidRange()
    {
        var readings = await _sensor.GetReadingsAsync();
        var value = double.Parse(readings[0].Value);
        Assert.InRange(value, 0.0, 100.0);
    }
}
