using HassLink.Config;

namespace HassLink.Tests;

/// <summary>
/// Verifies that AppConfig defaults are what the application and its
/// documentation promise. A regression here would ship with wrong defaults
/// (e.g. a sensor accidentally disabled, or the wrong port).
/// </summary>
public class AppConfigDefaultsTests
{
    [Fact]
    public void DefaultPort_Is1883()
    {
        Assert.Equal(1883, new MqttConfig().Port);
    }

    [Fact]
    public void DefaultBaseTopic_IsHassLink()
    {
        Assert.Equal("hass-link", new MqttConfig().BaseTopic);
    }

    [Fact]
    public void DefaultPublishInterval_Is30Seconds()
    {
        Assert.Equal(30, new AppConfig().PublishIntervalSeconds);
    }

    [Fact]
    public void DefaultPassword_IsEmpty()
    {
        Assert.Equal("", new MqttConfig().Password);
        Assert.Equal("", new MqttConfig().EncryptedPassword);
    }

    [Fact]
    public void IsConfigured_FalseWithNoHost()
    {
        Assert.False(new MqttConfig().IsConfigured);
    }

    [Fact]
    public void IsConfigured_TrueWhenHostSet()
    {
        Assert.True(new MqttConfig { Host = "192.168.1.1" }.IsConfigured);
    }

    [Theory]
    [InlineData("cpu",          true)]
    [InlineData("ram",          true)]
    [InlineData("disk",         true)]
    [InlineData("network",      true)]
    [InlineData("activeWindow", true)]
    [InlineData("uptime",       true)]
    [InlineData("cpuTemp",      true)]
    [InlineData("battery",      false)]
    [InlineData("gpuTemp",      false)]
    public void SensorDefaults_MatchExpected(string sensorId, bool expectedEnabled)
    {
        var config = new AppConfig();
        Assert.Equal(expectedEnabled, config.GetSensor(sensorId).Enabled);
    }
}
