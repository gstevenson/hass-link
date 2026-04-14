namespace HassLink.Mqtt;

/// <summary>
/// Minimal publishing surface needed by HassDiscovery and SensorManager.
/// Abstracted from MqttService so these classes can be tested without a real broker.
/// </summary>
public interface IMqttPublisher
{
    bool IsConnected { get; }
    Task PublishAsync(string topic, string payload, bool retain = false);
}
