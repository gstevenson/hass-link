using System.Text.Json;
using HassLink.Config;
using HassLink.Mqtt;
using HassLink.Sensors;

namespace HassLink.Tests;

/// <summary>
/// Verifies that HassDiscovery publishes to the correct topics with the
/// correct payloads. Any regression here silently breaks Home Assistant
/// entity registration — entities would either disappear or duplicate.
/// </summary>
public class HassDiscoveryTests
{
    private readonly FakeMqttPublisher _publisher;
    private readonly AppConfig _config;
    private readonly HassDiscovery _discovery;

    public HassDiscoveryTests()
    {
        _publisher = new FakeMqttPublisher();
        _config = new AppConfig
        {
            DeviceName = "TestPC",
            Mqtt = new MqttConfig { BaseTopic = "hass-link" }
        };
        _discovery = new HassDiscovery(_publisher, _config);
    }

    // ── PublishAllAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task PublishAllAsync_UsesCorrectDiscoveryTopic()
    {
        await _discovery.PublishAllAsync(OneCpuSensor());

        var topic = DiscoveryMessage().Topic;
        Assert.Equal("homeassistant/sensor/testpc/cpu_usage/config", topic);
    }

    [Fact]
    public async Task PublishAllAsync_PayloadContainsCorrectStateTopic()
    {
        await _discovery.PublishAllAsync(OneCpuSensor());

        var payload = ParsePayload(DiscoveryMessage().Payload);
        Assert.Equal("hass-link/testpc/cpu_usage/state", payload["state_topic"].GetString());
    }

    [Fact]
    public async Task PublishAllAsync_PayloadContainsCorrectUniqueId()
    {
        await _discovery.PublishAllAsync(OneCpuSensor());

        var payload = ParsePayload(DiscoveryMessage().Payload);
        Assert.Equal("hasslink_testpc_cpu_usage", payload["unique_id"].GetString());
    }

    [Fact]
    public async Task PublishAllAsync_PayloadContainsCorrectAvailabilityTopics()
    {
        await _discovery.PublishAllAsync(OneCpuSensor());

        var payload = ParsePayload(DiscoveryMessage().Payload);
        var topics = payload["availability"].EnumerateArray()
            .Select(a => a.GetProperty("topic").GetString())
            .ToList();

        Assert.Contains("hass-link/testpc/status", topics);
        Assert.Contains("hass-link/testpc/cpu_usage/availability", topics);
        Assert.Equal("all", payload["availability_mode"].GetString());
    }

    [Fact]
    public async Task PublishAllAsync_PayloadContainsName()
    {
        await _discovery.PublishAllAsync(OneCpuSensor());

        var payload = ParsePayload(DiscoveryMessage().Payload);
        Assert.Equal("CPU Usage", payload["name"].GetString());
    }

    [Fact]
    public async Task PublishAllAsync_PayloadContainsUnit()
    {
        await _discovery.PublishAllAsync(OneCpuSensor());

        var payload = ParsePayload(DiscoveryMessage().Payload);
        Assert.Equal("%", payload["unit_of_measurement"].GetString());
    }

    [Fact]
    public async Task PublishAllAsync_PublishesWithRetainFlag()
    {
        await _discovery.PublishAllAsync(OneCpuSensor());

        Assert.True(DiscoveryMessage().Retain);
    }

    [Fact]
    public async Task PublishAllAsync_PublishesOneDiscoveryMessagePerReading()
    {
        var sensors = new[]
        {
            new FakeSensor("cpu_usage", "CPU Usage", "42", "%"),
            new FakeSensor("ram_usage", "RAM Usage", "55", "%"),
        };

        await _discovery.PublishAllAsync(sensors);

        var discoveryCount = _publisher.Published.Count(m => m.Topic.StartsWith("homeassistant/"));
        Assert.Equal(2, discoveryCount);
    }

    [Fact]
    public async Task PublishAllAsync_DeviceNameIsSanitisedInTopic()
    {
        _config.DeviceName = "My PC";
        await _discovery.PublishAllAsync(OneCpuSensor());

        var topic = DiscoveryMessage().Topic;
        Assert.StartsWith("homeassistant/sensor/my_pc/", topic);
    }

    [Fact]
    public async Task PublishAllAsync_WhenNotConnected_PublishesNothing()
    {
        _publisher.SimulateDisconnected = true;
        await _discovery.PublishAllAsync(OneCpuSensor());
        Assert.Empty(_publisher.Published);
    }

    // ── PublishAvailabilityAsync ────────────────────────────────────────────

    [Fact]
    public async Task PublishAvailabilityAsync_Online_PublishesCorrectTopic()
    {
        await _discovery.PublishAvailabilityAsync(online: true);

        Assert.Equal("hass-link/testpc/status", _publisher.Published.Single().Topic);
    }

    [Fact]
    public async Task PublishAvailabilityAsync_Online_PublishesOnlinePayload()
    {
        await _discovery.PublishAvailabilityAsync(online: true);

        Assert.Equal("online", _publisher.Published.Single().Payload);
    }

    [Fact]
    public async Task PublishAvailabilityAsync_Offline_PublishesOfflinePayload()
    {
        await _discovery.PublishAvailabilityAsync(online: false);

        Assert.Equal("offline", _publisher.Published.Single().Payload);
    }

    [Fact]
    public async Task PublishAvailabilityAsync_PublishesWithRetainFlag()
    {
        await _discovery.PublishAvailabilityAsync(online: true);
        Assert.True(_publisher.Published.Single().Retain);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private (string Topic, string Payload, bool Retain) DiscoveryMessage() =>
        _publisher.Published.Single(m => m.Topic.StartsWith("homeassistant/"));

    private static IReadOnlyList<ISensor> OneCpuSensor() =>
        [new FakeSensor("cpu_usage", "CPU Usage", "42", "%")];

    private static Dictionary<string, JsonElement> ParsePayload(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
}

/// <summary>Test double for IMqttPublisher — captures all publishes for assertion.</summary>
internal class FakeMqttPublisher : IMqttPublisher
{
    public bool SimulateDisconnected { get; set; }
    public bool IsConnected => !SimulateDisconnected;
    public List<(string Topic, string Payload, bool Retain)> Published { get; } = [];

    public Task PublishAsync(string topic, string payload, bool retain = false)
    {
        Published.Add((topic, payload, retain));
        return Task.CompletedTask;
    }
}

/// <summary>Test double for ISensor — returns a single fixed reading.</summary>
internal class FakeSensor(string sensorId, string name, string value, string? unit) : ISensor
{
    public string Id => sensorId;
    public string Name => name;
    public bool IsAvailable => true;

    public Task<IReadOnlyList<SensorReading>> GetReadingsAsync() =>
        Task.FromResult<IReadOnlyList<SensorReading>>(
            [new(sensorId, name, value, unit, null, null)]);

    public void Dispose() { }
}
