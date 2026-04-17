using HassLink.Config;
using HassLink.Sensors;

namespace HassLink.Tests;

public class SensorManagerTests : IDisposable
{
    private readonly FakeMqttPublisher _mqtt = new();
    private readonly AppConfig _config;
    private readonly SensorManager _manager;

    public SensorManagerTests()
    {
        _config = new AppConfig();
        foreach (var key in new[] { "cpu", "ram", "disk", "network", "activeWindow", "uptime", "battery", "cpuTemp", "gpuTemp" })
            _config.Sensors[key] = new SensorConfig { Enabled = false };

        _manager = new SensorManager(_mqtt, _config);
    }

    public void Dispose() => _manager.Dispose();

    [Fact]
    public void GetTimeUntilNextPublish_ReturnsNull_BeforeStart()
    {
        Assert.Null(_manager.GetTimeUntilNextPublish());
    }

    [Fact]
    public void GetSensors_IsEmpty_BeforeStart()
    {
        Assert.Empty(_manager.GetSensors());
    }

    [Fact]
    public void Start_WithAllSensorsDisabled_GetSensorsIsEmpty()
    {
        _manager.Start(_config);
        Assert.Empty(_manager.GetSensors());
    }

    [Fact]
    public void GetTimeUntilNextPublish_ReturnsNonNegativeValue_AfterStart()
    {
        _manager.Start(_config);
        var remaining = _manager.GetTimeUntilNextPublish();
        Assert.NotNull(remaining);
        Assert.True(remaining.Value >= TimeSpan.Zero);
    }

    [Fact]
    public void GetHardwareDiagnostics_WithNoHardwareSensor_ReturnsNotEnabledMessage()
    {
        var result = _manager.GetHardwareDiagnostics();
        Assert.Equal("Hardware sensor is not enabled.", result);
    }

    [Fact]
    public void Restart_WithAllSensorsDisabled_GetSensorsRemainsEmpty()
    {
        _manager.Start(_config);
        _manager.Restart(_config);
        Assert.Empty(_manager.GetSensors());
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        _manager.Start(_config);
        var ex = Record.Exception(() => _manager.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public async Task PublishAllAsync_WhenDisconnected_PublishesNothing()
    {
        _mqtt.SimulateDisconnected = true;
        _manager.Start(_config);

        await _manager.PublishAllAsync();

        Assert.Empty(_mqtt.Published);
    }

    [Fact]
    public async Task PublishAllAsync_WhenConnected_WithUptimeSensor_PublishesStateAndAvailability()
    {
        var config = new AppConfig { DeviceName = "TestPC" };
        foreach (var key in new[] { "cpu", "ram", "disk", "network", "activeWindow", "battery", "cpuTemp", "gpuTemp" })
            config.Sensors[key] = new SensorConfig { Enabled = false };
        config.Sensors["uptime"] = new SensorConfig { Enabled = true };

        using var manager = new SensorManager(_mqtt, config);
        manager.Start(config);

        await manager.PublishAllAsync();

        Assert.NotEmpty(_mqtt.Published);
        Assert.Contains(_mqtt.Published, m => m.Topic.Contains("uptime") && m.Topic.EndsWith("/state"));
        Assert.Contains(_mqtt.Published, m => m.Topic.Contains("uptime") && m.Topic.EndsWith("/availability"));
    }

    [Fact]
    public async Task PublishAllAsync_WhenConnected_PublishesRetainedAvailability()
    {
        var config = new AppConfig { DeviceName = "TestPC" };
        foreach (var key in new[] { "cpu", "ram", "disk", "network", "activeWindow", "battery", "cpuTemp", "gpuTemp" })
            config.Sensors[key] = new SensorConfig { Enabled = false };
        config.Sensors["uptime"] = new SensorConfig { Enabled = true };

        using var manager = new SensorManager(_mqtt, config);
        manager.Start(config);

        await manager.PublishAllAsync();

        var availMsg = _mqtt.Published.First(m => m.Topic.EndsWith("/availability"));
        Assert.True(availMsg.Retain);
        Assert.Equal("online", availMsg.Payload);
    }

    [Fact]
    public async Task PublishAllAsync_WhenConnected_UsesDeviceIdInTopic()
    {
        var config = new AppConfig { DeviceName = "My PC!" };
        foreach (var key in new[] { "cpu", "ram", "disk", "network", "activeWindow", "battery", "cpuTemp", "gpuTemp" })
            config.Sensors[key] = new SensorConfig { Enabled = false };
        config.Sensors["uptime"] = new SensorConfig { Enabled = true };

        using var manager = new SensorManager(_mqtt, config);
        manager.Start(config);

        await manager.PublishAllAsync();

        Assert.All(_mqtt.Published, m => Assert.Contains("my_pc", m.Topic));
    }
}
