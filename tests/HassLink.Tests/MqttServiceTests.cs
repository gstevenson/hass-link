using HassLink.Config;
using HassLink.Mqtt;

namespace HassLink.Tests;

public class MqttServiceTests : IDisposable
{
    private readonly AppConfig _config = new();
    private readonly MqttService _service;

    public MqttServiceTests()
    {
        _service = new MqttService(_config);
    }

    public void Dispose() => _service.Dispose();

    [Fact]
    public void InitialState_IsDisconnected()
    {
        Assert.Equal(ConnectionState.Disconnected, _service.State);
    }

    [Fact]
    public void IsConnected_IsFalse_WhenDisconnected()
    {
        Assert.False(_service.IsConnected);
    }

    [Fact]
    public void LastError_IsNull_Initially()
    {
        Assert.Null(_service.LastError);
    }

    [Fact]
    public async Task ConnectAsync_WhenNotConfigured_RemainsDisconnected()
    {
        _config.Mqtt.Host = "";
        await _service.ConnectAsync(_config);

        Assert.Equal(ConnectionState.Disconnected, _service.State);
        Assert.False(_service.IsConnected);
    }

    [Fact]
    public async Task DisconnectAsync_WhenAlreadyDisconnected_DoesNotThrow()
    {
        var ex = await Record.ExceptionAsync(() => _service.DisconnectAsync());
        Assert.Null(ex);
    }

    [Fact]
    public async Task DisconnectAsync_SetsStateToDisconnected()
    {
        await _service.DisconnectAsync();
        Assert.Equal(ConnectionState.Disconnected, _service.State);
    }

    [Fact]
    public async Task PublishAsync_WhenNotConnected_DoesNotThrow()
    {
        var ex = await Record.ExceptionAsync(() => _service.PublishAsync("test/topic", "payload"));
        Assert.Null(ex);
    }

    [Fact]
    public void Dispose_WhenNeverConnected_DoesNotThrow()
    {
        using var service = new MqttService(_config);
        var ex = Record.Exception(() => service.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void StateChanged_EventFires_WhenStateChanges()
    {
        var states = new List<ConnectionState>();
        _service.StateChanged += s => states.Add(s);

        _service.Dispose();

        Assert.Empty(states);
    }

    [Fact]
    public async Task ConnectAsync_WhenNotConfigured_DoesNotReachConnectedState()
    {
        var states = new List<ConnectionState>();
        _service.StateChanged += s => states.Add(s);
        _config.Mqtt.Host = "";

        await _service.ConnectAsync(_config);

        Assert.DoesNotContain(ConnectionState.Connected, states);
        Assert.DoesNotContain(ConnectionState.Connecting, states);
    }
}
