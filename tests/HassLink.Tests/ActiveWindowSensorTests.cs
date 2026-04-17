using HassLink.Sensors;

namespace HassLink.Tests;

public class ActiveWindowSensorTests : IDisposable
{
    private readonly ActiveWindowSensor _sensor = new();

    public void Dispose() => _sensor.Dispose();

    [Fact]
    public void Id_IsActiveWindow()
    {
        Assert.Equal("activeWindow", _sensor.Id);
    }

    [Fact]
    public void Name_IsActiveWindow()
    {
        Assert.Equal("Active Window", _sensor.Name);
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
    public async Task Reading_SensorIdIsActiveWindow()
    {
        var readings = await _sensor.GetReadingsAsync();
        Assert.Equal("active_window", readings[0].SensorId);
    }

    [Fact]
    public async Task Reading_ValueIsNotNull()
    {
        var readings = await _sensor.GetReadingsAsync();
        Assert.NotNull(readings[0].Value);
    }

    [Fact]
    public async Task Reading_UnitIsNull()
    {
        var readings = await _sensor.GetReadingsAsync();
        Assert.Null(readings[0].Unit);
    }

    [Fact]
    public async Task Reading_IconIsMdiApplication()
    {
        var readings = await _sensor.GetReadingsAsync();
        Assert.Equal("mdi:application", readings[0].Icon);
    }
}
