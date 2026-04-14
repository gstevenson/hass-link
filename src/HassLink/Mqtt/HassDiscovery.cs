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
/// </summary>
public class HassDiscovery
{
    private readonly MqttService _mqtt;
    private readonly AppConfig _config;

    public HassDiscovery(MqttService mqtt, AppConfig config)
    {
        _mqtt = mqtt;
        _config = config;
    }

    public async Task PublishAllAsync(IReadOnlyList<ISensor> sensors)
    {
        if (!_mqtt.IsConnected) return;

        var deviceId = SensorManager.SanitiseId(_config.DeviceName);
        var device = BuildDevicePayload(deviceId);

        // Collect all readings (we need the SensorReading metadata)
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

        var payload = new Dictionary<string, object?>
        {
            ["name"] = reading.Name,
            ["unique_id"] = $"hasslink_{deviceId}_{reading.SensorId}",
            ["state_topic"] = stateTopic,
            ["availability_topic"] = $"{_config.Mqtt.BaseTopic}/{deviceId}/status",
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
    /// Publishes the online/offline availability status for this device.
    /// Called on connect (online) and before disconnect (offline).
    /// </summary>
    public async Task PublishAvailabilityAsync(bool online)
    {
        if (!_mqtt.IsConnected) return;

        var deviceId = SensorManager.SanitiseId(_config.DeviceName);
        var topic = $"{_config.Mqtt.BaseTopic}/{deviceId}/status";
        await _mqtt.PublishAsync(topic, online ? "online" : "offline", retain: true);
    }
}
