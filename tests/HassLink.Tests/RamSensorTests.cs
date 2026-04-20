using HassLink.Sensors;

namespace HassLink.Tests;

public class RamSensorTests : IDisposable
{
    private readonly RamSensor _sensor = new();

    public void Dispose() => _sensor.Dispose();

    [Fact]
    public void Id_IsRam()
    {
        Assert.Equal("ram", _sensor.Id);
    }

    [Fact]
    public void Name_IsRamUsage()
    {
        Assert.Equal("RAM Usage", _sensor.Name);
    }

    [Fact]
    public void IsAvailable_IsTrue()
    {
        Assert.True(_sensor.IsAvailable);
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
        Assert.Contains(readings, r => r.SensorId == "ram_percent");
        Assert.Contains(readings, r => r.SensorId == "ram_used_gb");
        Assert.Contains(readings, r => r.SensorId == "ram_total_gb");
    }

    [Fact]
    public async Task Reading_ValuesAreParseableNumbers()
    {
        var readings = await _sensor.GetReadingsAsync();
        foreach (var r in readings)
            Assert.True(double.TryParse(r.Value, out _), $"'{r.Value}' for {r.SensorId} is not a valid number");
    }

    [Fact]
    public async Task Reading_PercentIsInRange()
    {
        var readings = await _sensor.GetReadingsAsync();
        var percent = double.Parse(readings.First(r => r.SensorId == "ram_percent").Value);
        Assert.InRange(percent, 0.0, 100.0);
    }

    [Fact]
    public async Task Reading_TotalGbIsPositive()
    {
        var readings = await _sensor.GetReadingsAsync();
        var total = double.Parse(readings.First(r => r.SensorId == "ram_total_gb").Value);
        Assert.True(total > 0, "Total RAM should be greater than 0");
    }

    [Fact]
    public async Task Reading_UsedGbIsNonNegative()
    {
        var readings = await _sensor.GetReadingsAsync();
        var used = double.Parse(readings.First(r => r.SensorId == "ram_used_gb").Value);
        Assert.True(used >= 0);
    }
}
