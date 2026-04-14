using System.Text.Json;
using HassLink.Config;
using HassLink.Sensors;

namespace HassLink.Mqtt;

/// <summary>
/// Publishes Home Assistant MQTT discovery config messages so sensors
/// appear automatically in HA without any manual configuration.
///
/// Discovery topic: homeassistant/sensor/{device_id}/{sensor_id}/config
/// State topic:     {baseTopic}/{device_id}/{sensor_id}/state
/// Availability:    {baseTopic}/{device_id}/status              (device-level)
///                  {baseTopic}/{device_id}/{sensor_id}/availability (per-sensor)
///
/// Both availability topics must be "online" for HA to show the sensor as available.
/// This lets us mark individual sensors unavailable when disabled, while the
/// device-level topic handles the whole-app online/offline state.
/// </summary>
public class HassDiscovery
{
    private readonly IMqttPublisher _mqtt;
    private readonly AppConfig _config;
    private readonly HashSet<string> _publishedSensorIds = [];

    public HassDiscovery(IMqttPublisher mqtt, AppConfig config)
    {
        _mqtt = mqtt;
        _config = config;
    }

    public async Task PublishAllAsync(IReadOnlyList<ISensor> sensors)
    {
        if (!_mqtt.IsConnected) return;

        var deviceId = SensorManager.SanitiseId(_config.DeviceName);
        var device = BuildDevicePayload(deviceId);

        foreach (var sensor in sensors)
        {
            try
            {
                var readings = await sensor.GetReadingsAsync();
                foreach (var reading in readings)
                    await PublishDiscoveryAsync(deviceId, reading, device);
            }
            catch
            {
                // Skip sensors that fail during discovery enumeration
            }
        }
    }

    private async Task PublishDiscoveryAsync(
        string deviceId,
        SensorReading reading,
        Dictionary<string, object> device)
    {
        var stateTopic = $"{_config.Mqtt.BaseTopic}/{deviceId}/{reading.SensorId}/state";
        var deviceStatusTopic = $"{_config.Mqtt.BaseTopic}/{deviceId}/status";
        var sensorAvailTopic = $"{_config.Mqtt.BaseTopic}/{deviceId}/{reading.SensorId}/availability";

        var payload = new Dictionary<string, object?>
        {
            ["name"] = reading.Name,
            ["unique_id"] = $"hasslink_{deviceId}_{reading.SensorId}",
            ["state_topic"] = stateTopic,
            ["availability"] = new object[]
            {
                new { topic = deviceStatusTopic },
                new { topic = sensorAvailTopic },
            },
            ["availability_mode"] = "all",
            ["payload_available"] = "online",
            ["payload_not_available"] = "offline",
            ["device"] = device,
        };

        if (reading.Unit is not null)
            payload["unit_of_measurement"] = reading.Unit;

        if (reading.DeviceClass is not null)
            payload["device_class"] = reading.DeviceClass;

        if (reading.Icon is not null)
            payload["icon"] = reading.Icon;

        var discoveryTopic = $"homeassistant/sensor/{deviceId}/{reading.SensorId}/config";
        var json = JsonSerializer.Serialize(payload);

        await _mqtt.PublishAsync(discoveryTopic, json, retain: true);

        // Mark this sensor available and track its ID
        _publishedSensorIds.Add(reading.SensorId);
        await _mqtt.PublishAsync(sensorAvailTopic, "online", retain: true);
    }

    private Dictionary<string, object> BuildDevicePayload(string deviceId)
    {
        return new Dictionary<string, object>
        {
            ["identifiers"] = new[] { $"hasslink_{deviceId}" },
            ["name"] = _config.DeviceName,
            ["manufacturer"] = "hass-link",
            ["model"] = Environment.OSVersion.VersionString,
            ["sw_version"] = Application.ProductVersion ?? "1.0.0",
        };
    }

    /// <summary>
    /// Publishes online/offline availability for the device and all known sensors.
    /// Called on connect (online) and before disconnect (offline).
    /// When going offline, all per-sensor availability topics are also marked offline
    /// so that sensors disabled between sessions don't retain a stale "online" state.
    /// </summary>
    public async Task PublishAvailabilityAsync(bool online)
    {
        if (!_mqtt.IsConnected) return;

        var deviceId = SensorManager.SanitiseId(_config.DeviceName);
        await _mqtt.PublishAsync($"{_config.Mqtt.BaseTopic}/{deviceId}/status", online ? "online" : "offline", retain: true);

        if (!online)
        {
            foreach (var sensorId in _publishedSensorIds)
            {
                var topic = $"{_config.Mqtt.BaseTopic}/{deviceId}/{sensorId}/availability";
                await _mqtt.PublishAsync(topic, "offline", retain: true);
            }
        }
    }
}
