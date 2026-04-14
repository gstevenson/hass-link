namespace HassLink.Sensors;

/// <summary>
/// Represents a single publishable sensor value.
/// A sensor implementation may produce multiple readings
/// (e.g. DiskSensor produces one per drive letter).
/// </summary>
public record SensorReading(
    string SensorId,    // unique within the device, used in MQTT topics
    string Name,        // human-readable name shown in Home Assistant
    string Value,       // the state value to publish
    string? Unit,       // e.g. "%", "GB", "°C"
    string? DeviceClass, // HA device_class (e.g. "temperature", "battery")
    string? Icon        // MDI icon, e.g. "mdi:cpu-64-bit"
);

public interface ISensor : IDisposable
{
    /// <summary>Stable short identifier, e.g. "cpu", "ram", "disk_c".</summary>
    string Id { get; }

    /// <summary>Display name shown in settings UI.</summary>
    string Name { get; }

    /// <summary>False if the sensor cannot run on this machine (e.g. no battery).</summary>
    bool IsAvailable { get; }

    /// <summary>Collect current readings. May return multiple values (disk, network).</summary>
    Task<IReadOnlyList<SensorReading>> GetReadingsAsync();
}
