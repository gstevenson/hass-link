using HassLink.Sensors;

namespace HassLink.Tests;

public class UptimeSensorTests
{
    private readonly UptimeSensor _sensor = new();

    [Fact]
    public async Task GetReadingsAsync_ReturnsExactlyOneReading()
    {
        var readings = await _sensor.GetReadingsAsync();
        Assert.Single(readings);
    }

    [Fact]
    public async Task Reading_HasCorrectSensorId()
    {
        var readings = await _sensor.GetReadingsAsync();
        Assert.Equal("uptime_hours", readings[0].SensorId);
    }

    [Fact]
    public async Task Reading_HasCorrectUnit()
    {
        var readings = await _sensor.GetReadingsAsync();
        Assert.Equal("h", readings[0].Unit);
    }

    [Fact]
    public async Task Reading_HasCorrectDeviceClass()
    {
        var readings = await _sensor.GetReadingsAsync();
        Assert.Equal("duration", readings[0].DeviceClass);
    }

    [Fact]
    public async Task Reading_ValueIsParseablePositiveNumber()
    {
        var readings = await _sensor.GetReadingsAsync();
        var parsed = double.Parse(readings[0].Value);
        Assert.True(parsed >= 0);
    }

    [Fact]
    public void IsAvailable_IsTrue()
    {
        Assert.True(_sensor.IsAvailable);
    }
}
