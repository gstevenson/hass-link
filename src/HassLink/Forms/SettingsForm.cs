using System.Security.Principal;
using HassLink.Config;
using HassLink.Mqtt;

namespace HassLink.Forms;

public class SettingsForm : Form
{
    private readonly AppConfig _config;
    private readonly Func<TimeSpan?> _getTimeUntilNextPublish;
    private System.Windows.Forms.Timer? _countdownTimer;
    private ToolStripStatusLabel _statusLabel = null!;

    // Connection tab controls
    private TextBox _tbHost = null!;
    private NumericUpDown _nudPort = null!;
    private TextBox _tbUsername = null!;
    private TextBox _tbPassword = null!;
    private CheckBox _cbTls = null!;
    private TextBox _tbBaseTopic = null!;
    private Button _btnTest = null!;
    private Label _lblTestResult = null!;

    // Sensors tab controls
    private CheckBox _cbCpu = null!;
    private CheckBox _cbRam = null!;
    private CheckBox _cbDisk = null!;
    private CheckBox _cbNetwork = null!;
    private CheckBox _cbActiveWindow = null!;
    private CheckBox _cbUptime = null!;
    private CheckBox _cbBattery = null!;
    private CheckBox _cbCpuTemp = null!;
    private CheckBox _cbGpuTemp = null!;

    // General tab controls
    private TextBox _tbDeviceName = null!;
    private NumericUpDown _nudInterval = null!;
    private CheckBox _cbStartWithWindows = null!;

    public AppConfig? SavedConfig { get; private set; }

    public SettingsForm(AppConfig config, Func<TimeSpan?> getTimeUntilNextPublish)
    {
        _config = config;
        _getTimeUntilNextPublish = getTimeUntilNextPublish;
        BuildUI();
        LoadConfig();
        StartCountdown();
    }

    private void BuildUI()
    {
        Text = "hass-link Settings";
        Size = new Size(480, 500);
        MinimumSize = new Size(420, 460);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(12, 6) };
        tabs.TabPages.Add(BuildConnectionTab());
        tabs.TabPages.Add(BuildSensorsTab());
        tabs.TabPages.Add(BuildGeneralTab());

        var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(8) };
        var btnSave = new Button { Text = "Save", DialogResult = DialogResult.OK, Width = 80 };
        var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 80 };
        btnSave.Click += OnSave;
        btnPanel.Controls.Add(btnSave);
        btnPanel.Controls.Add(btnCancel);
        btnSave.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
        btnCancel.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
        btnSave.Location = new Point(btnPanel.Width - 180, 10);
        btnCancel.Location = new Point(btnPanel.Width - 92, 10);

        _statusLabel = new ToolStripStatusLabel { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        var statusBar = new StatusStrip();
        statusBar.Items.Add(_statusLabel);

        Controls.Add(tabs);
        Controls.Add(btnPanel);
        Controls.Add(statusBar);
        AcceptButton = btnSave;
        CancelButton = btnCancel;
    }

    private void StartCountdown()
    {
        UpdateStatusLabel();
        _countdownTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _countdownTimer.Tick += (_, _) => UpdateStatusLabel();
        _countdownTimer.Start();
    }

    private void UpdateStatusLabel()
    {
        var remaining = _getTimeUntilNextPublish();
        _statusLabel.Text = remaining is null
            ? "Services not running"
            : $"Next publish in {(int)remaining.Value.TotalSeconds}s";
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _countdownTimer?.Stop();
        _countdownTimer?.Dispose();
        base.OnFormClosed(e);
    }

    private TabPage BuildConnectionTab()
    {
        var page = new TabPage("Connection");
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(12),
            AutoSize = true,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _tbHost = new TextBox { Dock = DockStyle.Fill };
        _nudPort = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1, Maximum = 65535, Value = 1883 };
        _tbUsername = new TextBox { Dock = DockStyle.Fill };
        _tbPassword = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
        _cbTls = new CheckBox { Text = "Use TLS/SSL", AutoSize = true };
        _tbBaseTopic = new TextBox { Dock = DockStyle.Fill };
        _btnTest = new Button { Text = "Test Connection", AutoSize = true };
        _lblTestResult = new Label { AutoSize = true, ForeColor = Color.Gray, Text = "" };

        _btnTest.Click += OnTestConnection;

        AddRow(layout, "MQTT Host:", _tbHost);
        AddRow(layout, "Port:", _nudPort);
        AddRow(layout, "Username:", _tbUsername);
        AddRow(layout, "Password:", _tbPassword);
        AddRow(layout, "", _cbTls);
        AddRow(layout, "Base Topic:", _tbBaseTopic);
        AddRow(layout, "", _btnTest);
        AddRow(layout, "", _lblTestResult);

        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildSensorsTab()
    {
        var page = new TabPage("Sensors");
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(12),
        };

        _cbCpu          = MakeSensorCheck("CPU Usage", "Overall CPU load %");
        _cbRam          = MakeSensorCheck("RAM Usage", "Memory used %, GB used, GB total");
        _cbDisk         = MakeSensorCheck("Disk Usage", "Free/used GB and % per drive");
        _cbNetwork      = MakeSensorCheck("Network Throughput", "Upload/download speed per adapter");
        _cbActiveWindow = MakeSensorCheck("Active Window", "Title of the currently focused window");
        _cbUptime       = MakeSensorCheck("System Uptime", "Hours since last boot");
        _cbBattery      = MakeSensorCheck("Battery", "Battery %, charging state (laptop only)");
        _cbCpuTemp      = MakeSensorCheck("CPU Temperature", "CPU package temp °C (requires admin)");
        _cbGpuTemp      = MakeSensorCheck("GPU Temperature / Load", "GPU temp °C and load % (requires admin)");

        var isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        var adminNote = new Label
        {
            Text = isAdmin
                ? "Running as Administrator — temperature sensors available."
                : "Not running as Administrator — CPU/GPU temperature sensors will not be available.",
            ForeColor = isAdmin ? Color.DarkGreen : Color.DarkOrange,
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0),
            MaximumSize = new Size(400, 0),
        };

        panel.Controls.AddRange([_cbCpu, _cbRam, _cbDisk, _cbNetwork, _cbActiveWindow, _cbUptime, _cbBattery, _cbCpuTemp, _cbGpuTemp, adminNote]);
        page.Controls.Add(panel);
        return page;
    }

    private TabPage BuildGeneralTab()
    {
        var page = new TabPage("General");
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(12),
            AutoSize = true,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _tbDeviceName = new TextBox { Dock = DockStyle.Fill };
        _nudInterval = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 5, Maximum = 3600, Value = 30 };
        _cbStartWithWindows = new CheckBox { Text = "Start with Windows", AutoSize = true };

        AddRow(layout, "Device Name:", _tbDeviceName);
        AddRow(layout, "Publish every (s):", _nudInterval);
        AddRow(layout, "", _cbStartWithWindows);

        var note = new Label
        {
            Text = "Device Name is used to identify this machine in Home Assistant. Use a unique name per device.",
            AutoSize = true,
            ForeColor = Color.Gray,
            MaximumSize = new Size(320, 0),
            Padding = new Padding(0, 8, 0, 0),
        };
        layout.SetColumnSpan(note, 2);
        layout.Controls.Add(new Label { Text = "" });
        layout.Controls.Add(note);

        page.Controls.Add(layout);
        return page;
    }

    private static CheckBox MakeSensorCheck(string name, string tooltip)
    {
        var cb = new CheckBox
        {
            Text = name,
            AutoSize = true,
            Padding = new Padding(0, 4, 0, 0),
        };
        new ToolTip().SetToolTip(cb, tooltip);
        return cb;
    }

    private static void AddRow(TableLayoutPanel layout, string label, Control control)
    {
        layout.Controls.Add(new Label
        {
            Text = label,
            TextAlign = ContentAlignment.MiddleRight,
            AutoSize = true,
            Padding = new Padding(0, 4, 4, 0),
        });
        layout.Controls.Add(control);
    }

    private void LoadConfig()
    {
        _tbHost.Text = _config.Mqtt.Host;
        _nudPort.Value = _config.Mqtt.Port;
        _tbUsername.Text = _config.Mqtt.Username;
        _tbPassword.Text = _config.Mqtt.Password;
        _cbTls.Checked = _config.Mqtt.UseTls;
        _tbBaseTopic.Text = _config.Mqtt.BaseTopic;
        _tbDeviceName.Text = _config.DeviceName;
        _nudInterval.Value = Math.Clamp(_config.PublishIntervalSeconds, 5, 3600);
        _cbStartWithWindows.Checked = _config.StartWithWindows;

        _cbCpu.Checked          = _config.GetSensor("cpu").Enabled;
        _cbRam.Checked          = _config.GetSensor("ram").Enabled;
        _cbDisk.Checked         = _config.GetSensor("disk").Enabled;
        _cbNetwork.Checked      = _config.GetSensor("network").Enabled;
        _cbActiveWindow.Checked = _config.GetSensor("activeWindow").Enabled;
        _cbUptime.Checked       = _config.GetSensor("uptime").Enabled;
        _cbBattery.Checked      = _config.GetSensor("battery").Enabled;
        _cbCpuTemp.Checked      = _config.GetSensor("cpuTemp").Enabled;
        _cbGpuTemp.Checked      = _config.GetSensor("gpuTemp").Enabled;
    }

    private void OnSave(object? sender, EventArgs e)
    {
        _config.Mqtt.Host = _tbHost.Text.Trim();
        _config.Mqtt.Port = (int)_nudPort.Value;
        _config.Mqtt.Username = _tbUsername.Text.Trim();
        _config.Mqtt.Password = _tbPassword.Text;
        _config.Mqtt.UseTls = _cbTls.Checked;
        _config.Mqtt.BaseTopic = _tbBaseTopic.Text.Trim().TrimEnd('/');

        _config.DeviceName = string.IsNullOrWhiteSpace(_tbDeviceName.Text)
            ? Environment.MachineName
            : _tbDeviceName.Text.Trim();
        _config.PublishIntervalSeconds = (int)_nudInterval.Value;
        _config.StartWithWindows = _cbStartWithWindows.Checked;

        _config.GetSensor("cpu").Enabled          = _cbCpu.Checked;
        _config.GetSensor("ram").Enabled          = _cbRam.Checked;
        _config.GetSensor("disk").Enabled         = _cbDisk.Checked;
        _config.GetSensor("network").Enabled      = _cbNetwork.Checked;
        _config.GetSensor("activeWindow").Enabled = _cbActiveWindow.Checked;
        _config.GetSensor("uptime").Enabled       = _cbUptime.Checked;
        _config.GetSensor("battery").Enabled      = _cbBattery.Checked;
        _config.GetSensor("cpuTemp").Enabled      = _cbCpuTemp.Checked;
        _config.GetSensor("gpuTemp").Enabled      = _cbGpuTemp.Checked;

        SavedConfig = _config;
        ConfigManager.Save(_config);
        Close();
    }

    private async void OnTestConnection(object? sender, EventArgs e)
    {
        _btnTest.Enabled = false;
        _lblTestResult.ForeColor = Color.Gray;
        _lblTestResult.Text = "Testing...";

        var testCfg = new MqttConfig
        {
            Host = _tbHost.Text.Trim(),
            Port = (int)_nudPort.Value,
            Username = _tbUsername.Text.Trim(),
            Password = _tbPassword.Text,
            UseTls = _cbTls.Checked,
        };

        var (success, error) = await MqttService.TestConnectionAsync(testCfg);

        _btnTest.Enabled = true;
        _lblTestResult.ForeColor = success ? Color.DarkGreen : Color.DarkRed;
        _lblTestResult.Text = success ? "Connected successfully!" : $"Failed: {error}";
    }
}
