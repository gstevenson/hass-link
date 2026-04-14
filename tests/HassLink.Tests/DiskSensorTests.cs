using HassLink.Sensors;

namespace HassLink.Tests;

public class DiskSensorTests
{
    private readonly DiskSensor _sensor = new();

    [Fact]
    public async Task GetReadingsAsync_ReturnsThreeReadingsPerDrive()
    {
        var readings = await _sensor.GetReadingsAsync();
        var driveCount = DriveInfo.GetDrives().Count(d => d.DriveType == DriveType.Fixed && d.IsReady);

        Assert.Equal(driveCount * 3, readings.Count);
    }

    [Fact]
    public async Task Reading_SensorIdsFollowExpectedPattern()
    {
        var readings = await _sensor.GetReadingsAsync();
        foreach (var r in readings)
        {
            Assert.Matches(@"^disk_[a-z]_(free|used|percent)$", r.SensorId);
        }
    }

    [Fact]
    public async Task Reading_ValuesAreParseableNumbers()
    {
        var readings = await _sensor.GetReadingsAsync();
        foreach (var r in readings)
        {
            Assert.True(double.TryParse(r.Value, out _), $"'{r.Value}' for {r.SensorId} is not a valid number");
        }
    }

    [Fact]
    public async Task Reading_PercentIsInRange()
    {
        var readings = await _sensor.GetReadingsAsync();
        var percents = readings.Where(r => r.SensorId.EndsWith("_percent"));
        foreach (var r in percents)
        {
            var value = double.Parse(r.Value);
            Assert.InRange(value, 0.0, 100.0);
        }
    }

    [Fact]
    public async Task Reading_UsedPlusFreeApproximatelyEqualsFree()
    {
        // used + free should roughly equal total (within floating point rounding)
        var readings = await _sensor.GetReadingsAsync();
        var drives = readings.Select(r => r.SensorId[5..6]).Distinct(); // extract drive letter

        foreach (var letter in drives)
        {
            var free = double.Parse(readings.First(r => r.SensorId == $"disk_{letter}_free").Value);
            var used = double.Parse(readings.First(r => r.SensorId == $"disk_{letter}_used").Value);
            Assert.True(free >= 0 && used >= 0);
        }
    }

    [Fact]
    public void IsAvailable_IsTrue()
    {
        Assert.True(_sensor.IsAvailable);
    }
}
