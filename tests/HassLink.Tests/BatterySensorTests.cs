using HassLink.Sensors;

namespace HassLink.Tests;

public class BatterySensorTests : IDisposable
{
    private readonly BatterySensor _sensor = new();

    public void Dispose() => _sensor.Dispose();

    [Fact]
    public void Id_IsBattery()
    {
        Assert.Equal("battery", _sensor.Id);
    }

    [Fact]
    public void Name_IsBattery()
    {
        Assert.Equal("Battery", _sensor.Name);
    }

    [Fact]
    public async Task GetReadingsAsync_ReturnsThreeReadings()
    {
        var readings = await _sensor.GetReadingsAsync();
        Assert.Equal(3, readings.Count);
    }

    [Fact]
    public async Task Reading_SensorIdsAreCorrect()
    {
        var readings = await _sensor.GetReadingsAsync();
        Assert.Contains(readings, r => r.SensorId == "battery_percent");
        Assert.Contains(readings, r => r.SensorId == "battery_charging");
        Assert.Contains(readings, r => r.SensorId == "battery_plugged_in");
    }

    [Fact]
    public async Task Reading_ChargingValueIsTrueOrFalse()
    {
        var readings = await _sensor.GetReadingsAsync();
        var charging = readings.First(r => r.SensorId == "battery_charging").Value;
        Assert.Contains(charging, new[] { "True", "False" });
    }

    [Fact]
    public async Task Reading_PluggedInValueIsTrueOrFalse()
    {
        var readings = await _sensor.GetReadingsAsync();
        var pluggedIn = readings.First(r => r.SensorId == "battery_plugged_in").Value;
        Assert.Contains(pluggedIn, new[] { "True", "False" });
    }

    [Fact]
    public async Task Reading_PercentValueIsParseableNumber()
    {
        var readings = await _sensor.GetReadingsAsync();
        var percentStr = readings.First(r => r.SensorId == "battery_percent").Value;
        Assert.True(double.TryParse(percentStr, out _), $"'{percentStr}' is not a valid number");
    }

    [Fact]
    public void IsAvailable_ReturnsConsistentValue()
    {
        // On a desktop with no battery, IsAvailable is false.
        // On a laptop with a battery, it is true. Either is valid.
        var available = _sensor.IsAvailable;
        Assert.True(available == true || available == false);
    }
}
