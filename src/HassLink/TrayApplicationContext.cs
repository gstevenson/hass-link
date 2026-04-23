using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using HassLink.Commands;
using HassLink.Config;
using HassLink.Forms;
using HassLink.Mqtt;
using HassLink.Sensors;
using Microsoft.Win32;

namespace HassLink;

/// <summary>
/// ApplicationContext is the WinForms equivalent of "the app" — it owns
/// app lifetime without requiring a visible window. Think of it as a
/// headless process that only surfaces a system-tray icon.
/// </summary>
[ExcludeFromCodeCoverage]
public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _statusItem;
    private Icon? _currentTrayIcon;
    private AppConfig _config;
    private MqttService? _mqtt;
    private SensorManager? _sensorManager;
    private CommandManager? _commandManager;
    private HassDiscovery? _discovery;
    private SettingsForm? _settingsForm;
    private AboutForm? _aboutForm;

    public TrayApplicationContext()
    {
        _config = ConfigManager.Load();

        _statusItem = new ToolStripMenuItem("Disconnected") { Enabled = false };

        var menu = new ContextMenuStrip();
        menu.Items.Add(_statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Settings...", null, OnOpenSettings);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("About", null, OnAbout);
        menu.Items.Add("Exit", null, OnExit);

        _currentTrayIcon = BuildTrayIcon(connected: false);
        _trayIcon = new NotifyIcon
        {
            Icon = _currentTrayIcon,
            Text = "hass-link",
            Visible = true,
            ContextMenuStrip = menu,
        };
        _trayIcon.DoubleClick += OnOpenSettings;

        // Always start services if MQTT is configured.
        if (_config.Mqtt.IsConfigured)
            _ = StartServicesAsync();

        // Show settings on first run (no MQTT configured) or if the user hasn't
        // opted into background start.
        if (!_config.Mqtt.IsConfigured || !_config.StartInBackground)
            ShowSettings();
    }

    private async Task StartServicesAsync()
    {
        _mqtt = new MqttService(_config);
        _mqtt.StateChanged += OnMqttStateChanged;

        _sensorManager = new SensorManager(_mqtt, _config);
        _sensorManager.Start(_config);

        _commandManager = new CommandManager(_mqtt, _config);

        await _mqtt.ConnectAsync(_config);
        // Discovery and subscriptions are set up in OnMqttConnectedAsync, triggered by StateChanged.
    }

    private async Task StopServicesAsync()
    {
        _sensorManager?.Dispose();
        _sensorManager = null;

        if (_commandManager is not null)
        {
            await _commandManager.StopAsync();
            _commandManager.Dispose();
            _commandManager = null;
        }

        if (_discovery is not null && (_mqtt?.IsConnected ?? false))
            await _discovery.PublishAvailabilityAsync(online: false);

        if (_mqtt is not null)
        {
            _mqtt.StateChanged -= OnMqttStateChanged;
            await _mqtt.DisconnectAsync();
            _mqtt.Dispose();
            _mqtt = null;
        }

        _discovery = null;
    }

    private void OnMqttStateChanged(ConnectionState state)
    {
        // Marshal UI updates to the WinForms thread (equivalent to React's setState from a worker)
        if (_trayIcon.ContextMenuStrip?.InvokeRequired ?? false)
        {
            _trayIcon.ContextMenuStrip.Invoke(() => OnMqttStateChanged(state));
            return;
        }

        var (text, icon) = state switch
        {
            ConnectionState.Connected    => ($"Connected to {_config.Mqtt.Host}", true),
            ConnectionState.Connecting   => ("Connecting...", false),
            ConnectionState.Error        => ("Connection failed", false),
            _                            => ("Disconnected", false),
        };

        _statusItem.Text = text;
        var old = _currentTrayIcon;
        _currentTrayIcon = BuildTrayIcon(icon);
        _trayIcon.Icon = _currentTrayIcon;
        old?.Dispose();
        _trayIcon.Text = $"hass-link — {text}";

        if (state == ConnectionState.Connected)
            _ = OnMqttConnectedAsync();
    }

    private async Task OnMqttConnectedAsync()
    {
        if (_mqtt is null || _sensorManager is null) return;
        _discovery = new HassDiscovery(_mqtt, _config);
        await _discovery.PublishAvailabilityAsync(online: true);
        await _discovery.PublishAllAsync(_sensorManager.GetSensors());
        await _discovery.PublishCommandDiscoveryAsync(_config.Commands);

        if (_commandManager is not null)
            await _commandManager.StartAsync();
    }

    private void OnOpenSettings(object? sender, EventArgs e)
    {
        if (_settingsForm is not null && !_settingsForm.IsDisposed)
        {
            _settingsForm.BringToFront();
            return;
        }

        ShowSettings();
    }

    private void ShowSettings()
    {
        _settingsForm = new SettingsForm(_config, () => _sensorManager?.GetTimeUntilNextPublish());
        _settingsForm.FormClosed += OnSettingsClosed;
        _settingsForm.SettingsApplied += OnSettingsApplied;
        _settingsForm.Show();
    }

    private void OnSettingsApplied(object? sender, EventArgs e)
    {
        if (_settingsForm?.SavedConfig is null) return;
        var oldConfig = _config;
        _config = _settingsForm.SavedConfig;
        ApplyStartWithWindows(_config.StartWithWindows);

        if (HasConnectionSettingsChanged(oldConfig, _config))
            _ = RestartServicesAsync();
        else
            _ = ApplySensorSettingsAsync();
    }

    private void OnSettingsClosed(object? sender, FormClosedEventArgs e)
    {
        if (_settingsForm?.SavedConfig is not null)
            OnSettingsApplied(sender, e);
    }

    private static bool HasConnectionSettingsChanged(AppConfig old, AppConfig next) =>
        old.Mqtt.Host != next.Mqtt.Host
        || old.Mqtt.Port != next.Mqtt.Port
        || old.Mqtt.Username != next.Mqtt.Username
        || old.Mqtt.EncryptedPassword != next.Mqtt.EncryptedPassword
        || old.Mqtt.UseTls != next.Mqtt.UseTls
        || old.Mqtt.BaseTopic != next.Mqtt.BaseTopic
        || old.DeviceName != next.DeviceName;

    private async Task ApplySensorSettingsAsync()
    {
        if (_sensorManager is null) return;
        _sensorManager.Restart(_config);

        if (_commandManager is not null)
            await _commandManager.RestartAsync(_config);

        if (_mqtt?.IsConnected == true)
        {
            if (_discovery is not null)
            {
                await _discovery.PublishAllSensorsOfflineAsync();
                var enabledCommandIds = _config.Commands
                    .Where(kv => kv.Value.Enabled)
                    .Select(kv => kv.Key);
                await _discovery.CleanupCommandsAsync(enabledCommandIds);
            }

            _discovery = new HassDiscovery(_mqtt, _config);
            await _discovery.PublishAvailabilityAsync(online: true);
            await _discovery.PublishAllAsync(_sensorManager.GetSensors());
            await _discovery.PublishCommandDiscoveryAsync(_config.Commands);
        }
    }

    private async Task RestartServicesAsync()
    {
        try
        {
            await StopServicesAsync();
            await StartServicesAsync();
        }
        catch (Exception ex)
        {
            _statusItem.Text = $"Restart failed: {ex.Message}";
        }
    }

    private void OnAbout(object? sender, EventArgs e)
    {
        if (_aboutForm is not null && !_aboutForm.IsDisposed)
        {
            _aboutForm.BringToFront();
            return;
        }

        _aboutForm = new AboutForm(BuildDiagnosticReport);
        _aboutForm.FormClosed += (_, _) => _aboutForm = null;
        _aboutForm.Show();
    }

    private string BuildDiagnosticReport()
    {
        var sb = new System.Text.StringBuilder();
        var version = Application.ProductVersion.Split('+')[0];
        var isAdmin = new System.Security.Principal.WindowsPrincipal(
            System.Security.Principal.WindowsIdentity.GetCurrent())
            .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

        sb.AppendLine("=== Application ===");
        sb.AppendLine($"Version:   {version}");
        sb.AppendLine($"OS:        {Environment.OSVersion}");
        sb.AppendLine($"Admin:     {(isAdmin ? "Yes" : "No")}");
        sb.AppendLine();

        sb.AppendLine("=== Configuration ===");
        sb.AppendLine($"Device name:      {_config.DeviceName}");
        sb.AppendLine($"Publish interval: {_config.PublishIntervalSeconds}s");
        sb.AppendLine($"MQTT host:        {_config.Mqtt.Host}");
        sb.AppendLine($"MQTT port:        {_config.Mqtt.Port}");
        sb.AppendLine($"MQTT username:    {(_config.Mqtt.Username is { Length: > 0 } u ? u : "(none)")}");
        sb.AppendLine($"MQTT password:    {(_config.Mqtt.EncryptedPassword is { Length: > 0 } ? "****" : "(none)")}");
        sb.AppendLine($"TLS:              {(_config.Mqtt.UseTls ? "Yes" : "No")}");
        sb.AppendLine($"Base topic:       {_config.Mqtt.BaseTopic}");
        sb.AppendLine();

        sb.AppendLine("=== Sensors ===");
        foreach (var (id, cfg) in _config.Sensors)
            sb.AppendLine($"{id,-14} {(cfg.Enabled ? "enabled" : "disabled")}");
        sb.AppendLine();

        sb.AppendLine("=== Hardware ===");
        sb.AppendLine(_sensorManager?.GetHardwareDiagnostics() ?? "Services not running.");

        return sb.ToString();
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _ = ShutdownAsync();
    }

    private async Task ShutdownAsync()
    {
        _trayIcon.Visible = false;
        await StopServicesAsync();
        _trayIcon.Dispose();
        Application.Exit();
    }

    private static void ApplyStartWithWindows(bool enable)
    {
        const string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        using var key = Registry.CurrentUser.OpenSubKey(keyPath, writable: true);
        if (key is null) return;

        if (enable)
            key.SetValue("hass-link", $"\"{Application.ExecutablePath}\"");
        else
            key.DeleteValue("hass-link", throwOnMissingValue: false);
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);

    /// <summary>
    /// Draws a simple dot icon — green when connected, grey when not.
    /// Avoids needing an .ico file to run the app.
    /// </summary>
    private static Icon BuildTrayIcon(bool connected)
    {
        using var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);

        var color = connected ? Color.FromArgb(34, 197, 94) : Color.FromArgb(120, 120, 120);
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 2, 2, 12, 12);

        // GetHicon allocates a native GDI handle that Icon.FromHandle does not own.
        // Clone to a fully managed Icon, then release the GDI handle immediately.
        var hIcon = bmp.GetHicon();
        try
        {
            return (Icon)Icon.FromHandle(hIcon).Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _currentTrayIcon?.Dispose();
            _trayIcon.Dispose();
            _mqtt?.Dispose();
            _sensorManager?.Dispose();
            _commandManager?.Dispose();
        }
        base.Dispose(disposing);
    }
}
