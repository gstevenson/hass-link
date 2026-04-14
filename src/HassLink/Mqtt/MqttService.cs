using System.Net.Security;
using System.Security.Authentication;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using HassLink.Config;

namespace HassLink.Mqtt;

public enum ConnectionState { Disconnected, Connecting, Connected, Error }

public class MqttService : IDisposable
{
    private IMqttClient? _client;
    private MqttClientOptions? _options;
    private CancellationTokenSource? _reconnectCts;
    private AppConfig _config;

    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
    public string? LastError { get; private set; }
    public bool IsConnected => State == ConnectionState.Connected && (_client?.IsConnected ?? false);

    public event Action<ConnectionState>? StateChanged;

    public MqttService(AppConfig config)
    {
        _config = config;
    }

    public async Task ConnectAsync(AppConfig config)
    {
        _config = config;

        await DisconnectAsync();

        if (!config.Mqtt.IsConfigured) return;

        _reconnectCts = new CancellationTokenSource();
        _ = ReconnectLoopAsync(_reconnectCts.Token);
        await Task.CompletedTask;
    }

    private async Task ReconnectLoopAsync(CancellationToken ct)
    {
        var backoffSeconds = 5;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                SetState(ConnectionState.Connecting);
                await DoConnectAsync(ct);
                SetState(ConnectionState.Connected);
                LastError = null;
                backoffSeconds = 5; // reset on success

                // Block here until disconnected or cancelled
                while (!ct.IsCancellationRequested && (_client?.IsConnected ?? false))
                    await Task.Delay(2000, ct);

                if (ct.IsCancellationRequested) break;

                SetState(ConnectionState.Disconnected);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                SetState(ConnectionState.Error);
                await Task.Delay(TimeSpan.FromSeconds(backoffSeconds), ct).ContinueWith(_ => { });
                backoffSeconds = Math.Min(backoffSeconds * 2, 120);
            }
        }

        SetState(ConnectionState.Disconnected);
    }

    private async Task DoConnectAsync(CancellationToken ct)
    {
        var factory = new MqttFactory();
        _client?.Dispose();
        _client = factory.CreateMqttClient();

        var builder = new MqttClientOptionsBuilder()
            .WithTcpServer(_config.Mqtt.Host, _config.Mqtt.Port)
            .WithClientId(_config.Mqtt.ClientId)
            .WithCleanSession(true)
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(30));

        if (!string.IsNullOrEmpty(_config.Mqtt.Username))
            builder.WithCredentials(_config.Mqtt.Username, _config.Mqtt.Password);

        if (_config.Mqtt.UseTls)
            builder.WithTlsOptions(o =>
            {
                o.UseTls();
                o.WithCertificateValidationHandler(ctx => ctx.SslPolicyErrors == SslPolicyErrors.None);
                o.WithSslProtocols(SslProtocols.Tls12 | SslProtocols.Tls13);
            });

        _options = builder.Build();
        await _client.ConnectAsync(_options, ct);
    }

    public async Task DisconnectAsync()
    {
        if (_reconnectCts is not null)
        {
            await _reconnectCts.CancelAsync();
            _reconnectCts.Dispose();
            _reconnectCts = null;
        }

        if (_client?.IsConnected ?? false)
        {
            try { await _client.DisconnectAsync(); }
            catch { /* ignore */ }
        }

        SetState(ConnectionState.Disconnected);
    }

    public async Task PublishAsync(string topic, string payload, bool retain = false)
    {
        if (!IsConnected || _client is null) return;

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag(retain)
            .Build();

        await _client.PublishAsync(message);
    }

    /// <summary>One-shot connection test — connects, then immediately disconnects.</summary>
    public static async Task<(bool Success, string? Error)> TestConnectionAsync(MqttConfig cfg)
    {
        try
        {
            var factory = new MqttFactory();
            using var client = factory.CreateMqttClient();

            var builder = new MqttClientOptionsBuilder()
                .WithTcpServer(cfg.Host, cfg.Port)
                .WithClientId($"hass-link-test-{Guid.NewGuid():N}")
                .WithCleanSession(true)
                .WithTimeout(TimeSpan.FromSeconds(5));

            if (!string.IsNullOrEmpty(cfg.Username))
                builder.WithCredentials(cfg.Username, cfg.Password);

            if (cfg.UseTls)
                builder.WithTlsOptions(o =>
                {
                    o.UseTls();
                    o.WithCertificateValidationHandler(ctx => ctx.SslPolicyErrors == SslPolicyErrors.None);
                    o.WithSslProtocols(SslProtocols.Tls12 | SslProtocols.Tls13);
                });

            var options = builder.Build();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            await client.ConnectAsync(options, cts.Token);
            await client.DisconnectAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private void SetState(ConnectionState state)
    {
        State = state;
        StateChanged?.Invoke(state);
    }

    public void Dispose()
    {
        _reconnectCts?.Cancel();
        _reconnectCts?.Dispose();
        _client?.Dispose();
    }
}
