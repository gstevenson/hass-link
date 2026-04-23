using System.Text.Json.Serialization;

namespace HassLink.Config;

public class AppConfig
{
    public MqttConfig Mqtt { get; set; } = new();
    public int PublishIntervalSeconds { get; set; } = 30;
    public string DeviceName { get; set; } = Environment.MachineName;
    public bool StartWithWindows { get; set; } = false;
    public bool StartInBackground { get; set; } = false;
    public Dictionary<string, SensorConfig> Sensors { get; set; } = DefaultSensors();

    private static Dictionary<string, SensorConfig> DefaultSensors() => new()
    {
        ["cpu"]          = new SensorConfig { Enabled = true },
        ["ram"]          = new SensorConfig { Enabled = true },
        ["disk"]         = new SensorConfig { Enabled = true },
        ["network"]      = new SensorConfig { Enabled = true },
        ["activeWindow"] = new SensorConfig { Enabled = true },
        ["uptime"]       = new SensorConfig { Enabled = true },
        ["battery"]      = new SensorConfig { Enabled = false },
        ["cpuTemp"]      = new SensorConfig { Enabled = true },
        ["gpuTemp"]      = new SensorConfig { Enabled = false },
    };

    public Dictionary<string, CommandConfig> Commands { get; set; } = DefaultCommands();

    private static Dictionary<string, CommandConfig> DefaultCommands() => new()
    {
        ["shutdown"]  = new CommandConfig { Type = "shutdown",  Name = "Shutdown" },
        ["restart"]   = new CommandConfig { Type = "restart",   Name = "Restart" },
        ["sleep"]     = new CommandConfig { Type = "sleep",     Name = "Sleep" },
        ["hibernate"] = new CommandConfig { Type = "hibernate", Name = "Hibernate" },
        ["lock"]      = new CommandConfig { Type = "lock",      Name = "Lock Screen" },
    };

    public SensorConfig GetSensor(string id)
    {
        if (!Sensors.TryGetValue(id, out var cfg))
        {
            cfg = new SensorConfig();
            Sensors[id] = cfg;
        }
        return cfg;
    }
}

public class MqttConfig
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 1883;
    public string Username { get; set; } = "";

    /// <summary>Plaintext password for runtime use only — never serialized.</summary>
    [JsonIgnore]
    public string Password { get; set; } = "";

    /// <summary>DPAPI-encrypted, base64-encoded password written to config.json.</summary>
    public string EncryptedPassword { get; set; } = "";

    public string ClientId { get; set; } = $"hass-link-{Environment.MachineName}";
    public bool UseTls { get; set; } = false;
    public string BaseTopic { get; set; } = "hass-link";

    [JsonIgnore]
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);
}

public class SensorConfig
{
    public bool Enabled { get; set; } = true;
}

public class CommandConfig
{
    public bool Enabled { get; set; } = false;
    public string Type { get; set; } = "custom";
    public string Name { get; set; } = "";
    public string? Executable { get; set; }
    public string? Arguments { get; set; }
}
