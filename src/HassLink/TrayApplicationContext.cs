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
public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _statusItem;
    private AppConfig _config;
    private MqttService? _mqtt;
    private SensorManager? _sensorManager;
    private HassDiscovery? _discovery;
    private SettingsForm? _settingsForm;

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

        _trayIcon = new NotifyIcon
        {
            Icon = BuildTrayIcon(connected: false),
            Text = "hass-link",
            Visible = true,
            ContextMenuStrip = menu,
        };
        _trayIcon.DoubleClick += OnOpenSettings;

        // Show settings on first run (no MQTT host configured yet)
        if (!_config.Mqtt.IsConfigured)
        {
            ShowSettings();
        }
        else
        {
            _ = StartServicesAsync();
        }
    }

    private async Task StartServicesAsync()
    {
        _mqtt = new MqttService(_config);
        _mqtt.StateChanged += OnMqttStateChanged;

        _sensorManager = new SensorManager(_mqtt, _config);
        _sensorManager.Start(_config);

        await _mqtt.ConnectAsync(_config);
        // Discovery is published in OnMqttConnectedAsync, triggered by the StateChanged event.
    }

    private async Task StopServicesAsync()
    {
        _sensorManager?.Dispose();
        _sensorManager = null;

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
            ConnectionState.Error        => ($"Error: {_mqtt?.LastError ?? "unknown"}", false),
            _                            => ("Disconnected", false),
        };

        _statusItem.Text = text;
        _trayIcon.Icon = BuildTrayIcon(icon);
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
        _settingsForm = new SettingsForm(_config);
        _settingsForm.FormClosed += OnSettingsClosed;
        _settingsForm.Show();
    }

    private void OnSettingsClosed(object? sender, FormClosedEventArgs e)
    {
        if (_settingsForm?.SavedConfig is not null)
        {
            _config = _settingsForm.SavedConfig;
            ApplyStartWithWindows(_config.StartWithWindows);
            _ = RestartServicesAsync();
        }
    }

    private async Task RestartServicesAsync()
    {
        await StopServicesAsync();
        await StartServicesAsync();
    }

    private void OnAbout(object? sender, EventArgs e)
    {
        using var form = new AboutForm();
        form.ShowDialog();
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

    /// <summary>
    /// Draws a simple dot icon — green when connected, grey when not.
    /// Avoids needing an .ico file to run the app.
    /// </summary>
    private static Icon BuildTrayIcon(bool connected)
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);

        var color = connected ? Color.FromArgb(34, 197, 94) : Color.FromArgb(120, 120, 120);
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 2, 2, 12, 12);

        return Icon.FromHandle(bmp.GetHicon());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _trayIcon.Dispose();
            _mqtt?.Dispose();
            _sensorManager?.Dispose();
        }
        base.Dispose(disposing);
    }
}
