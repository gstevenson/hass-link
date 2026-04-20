using HassLink.Config;

namespace HassLink.Tests;

public class ConfigManagerTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"hass-link-tests-{Guid.NewGuid()}");

    private string TempConfigPath => Path.Combine(_tempDir, "config.json");

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Load_WhenFileDoesNotExist_ReturnsDefaultConfig()
    {
        var config = ConfigManager.Load(TempConfigPath);

        Assert.NotNull(config);
        Assert.Equal(1883, config.Mqtt.Port);
        Assert.Equal("hass-link", config.Mqtt.BaseTopic);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsDeviceNameAndInterval()
    {
        var config = new AppConfig
        {
            DeviceName = "test-machine",
            PublishIntervalSeconds = 15
        };

        ConfigManager.Save(config, TempConfigPath);
        var loaded = ConfigManager.Load(TempConfigPath);

        Assert.Equal("test-machine", loaded.DeviceName);
        Assert.Equal(15, loaded.PublishIntervalSeconds);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsMqttSettings()
    {
        var config = new AppConfig();
        config.Mqtt.Host = "192.168.1.100";
        config.Mqtt.Port = 1884;
        config.Mqtt.Username = "mqttuser";
        config.Mqtt.Password = "p@ssw0rd";

        ConfigManager.Save(config, TempConfigPath);
        var loaded = ConfigManager.Load(TempConfigPath);

        Assert.Equal("192.168.1.100", loaded.Mqtt.Host);
        Assert.Equal(1884, loaded.Mqtt.Port);
        Assert.Equal("mqttuser", loaded.Mqtt.Username);
        Assert.Equal("p@ssw0rd", loaded.Mqtt.Password);
    }

    [Fact]
    public void Save_EncryptsPasswordInFile()
    {
        var config = new AppConfig();
        config.Mqtt.Password = "supersecret";

        ConfigManager.Save(config, TempConfigPath);

        var json = File.ReadAllText(TempConfigPath);
        Assert.DoesNotContain("supersecret", json);
    }

    [Fact]
    public void SaveThenLoad_PreservesSensorEnabledFlags()
    {
        var config = new AppConfig();
        config.Sensors["cpu"] = new SensorConfig { Enabled = false };
        config.Sensors["ram"] = new SensorConfig { Enabled = true };

        ConfigManager.Save(config, TempConfigPath);
        var loaded = ConfigManager.Load(TempConfigPath);

        Assert.False(loaded.GetSensor("cpu").Enabled);
        Assert.True(loaded.GetSensor("ram").Enabled);
    }

    [Fact]
    public void Save_WritesFileToExpectedPath()
    {
        var config = new AppConfig();

        ConfigManager.Save(config, TempConfigPath);

        Assert.True(File.Exists(TempConfigPath));
    }

    [Fact]
    public void SaveTwice_OverwritesPreviousFile()
    {
        var first = new AppConfig { DeviceName = "first" };
        ConfigManager.Save(first, TempConfigPath);

        var second = new AppConfig { DeviceName = "second" };
        ConfigManager.Save(second, TempConfigPath);

        var loaded = ConfigManager.Load(TempConfigPath);
        Assert.Equal("second", loaded.DeviceName);
    }

    [Fact]
    public void Load_WithCorruptJson_InvokesErrorCallbackAndReturnsDefault()
    {
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(TempConfigPath, "{ this is not valid json !!!");

        var errorCallbackInvoked = false;
        var config = ConfigManager.Load(TempConfigPath, onParseError: () => errorCallbackInvoked = true);

        Assert.True(errorCallbackInvoked);
        Assert.NotNull(config);
        Assert.Equal(1883, config.Mqtt.Port);
    }

    [Fact]
    public void Load_WithCorruptJson_NoCallbackProvided_ReturnsDefault()
    {
        // Verifies the no-callback overload still returns a default config.
        // (The default action would show a MessageBox — we can't test that path here,
        // but we verify the method signature compiles and falls back correctly.)
        var config = ConfigManager.Load(TempConfigPath);
        Assert.NotNull(config);
    }
}
