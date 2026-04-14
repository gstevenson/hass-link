using HassLink.Sensors;

namespace HassLink.Tests;

/// <summary>
/// SanitiseId maps device names to MQTT-safe identifiers used in every
/// topic published to Home Assistant. Any regression here silently breaks
/// all entity IDs on the broker.
/// </summary>
public class SanitiseIdTests
{
    [Theory]
    [InlineData("MyPC",        "mypc")]
    [InlineData("my-pc",       "my_pc")]
    [InlineData("My PC",       "my_pc")]
    [InlineData("MY_PC",       "my_pc")]
    [InlineData("PC!#123",     "pc__123")]
    [InlineData("DESKTOP-ABC", "desktop_abc")]
    [InlineData("-leading",    "leading")]
    [InlineData("trailing-",   "trailing")]
    [InlineData("-both-",      "both")]
    [InlineData("a",           "a")]
    [InlineData("123",         "123")]
    public void KnownInputs_ReturnExpectedId(string input, string expected)
    {
        Assert.Equal(expected, SensorManager.SanitiseId(input));
    }

    [Fact]
    public void AllSpecialChars_ReturnsEmpty()
    {
        // Known edge case (audit finding L-3): names consisting entirely of
        // non-alphanumeric characters sanitise to an empty string.
        // Callers should guard against this.
        Assert.Equal("", SensorManager.SanitiseId("---"));
    }

    [Fact]
    public void EmptyString_ReturnsEmpty()
    {
        Assert.Equal("", SensorManager.SanitiseId(""));
    }

    [Fact]
    public void OutputIsAlwaysLowercase()
    {
        var result = SensorManager.SanitiseId("ALLCAPS");
        Assert.Equal(result, result.ToLower());
    }

    [Fact]
    public void OutputContainsNoSpaces()
    {
        var result = SensorManager.SanitiseId("name with spaces");
        Assert.DoesNotContain(" ", result);
    }
}
