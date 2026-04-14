using HassLink.Config;

namespace HassLink.Tests;

/// <summary>
/// Verifies the DPAPI encrypt/decrypt round-trip introduced to protect
/// the MQTT password at rest. A regression here would silently store
/// plaintext passwords or fail to load existing configs.
/// </summary>
public class PasswordEncryptionTests
{
    [Fact]
    public void RoundTrip_ReturnsOriginalPassword()
    {
        const string password = "correct-horse-battery-staple";
        var encrypted = ConfigManager.EncryptPassword(password);
        var decrypted = ConfigManager.DecryptPassword(encrypted);
        Assert.Equal(password, decrypted);
    }

    [Fact]
    public void EmptyPassword_RoundTripsToEmpty()
    {
        Assert.Equal("", ConfigManager.DecryptPassword(ConfigManager.EncryptPassword("")));
    }

    [Fact]
    public void EncryptedValue_IsNotPlaintext()
    {
        const string password = "mysecret";
        var encrypted = ConfigManager.EncryptPassword(password);
        Assert.NotEqual(password, encrypted);
        Assert.DoesNotContain(password, encrypted);
    }

    [Fact]
    public void EncryptedValue_IsBase64()
    {
        var encrypted = ConfigManager.EncryptPassword("test");
        // Should not throw — valid base64 is required for DecryptPassword to work
        var bytes = Convert.FromBase64String(encrypted);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void DecryptEmpty_ReturnsEmpty()
    {
        Assert.Equal("", ConfigManager.DecryptPassword(""));
    }

    [Fact]
    public void DecryptGarbage_ReturnsEmpty()
    {
        // Corrupted or migrated config should fail gracefully, not throw
        Assert.Equal("", ConfigManager.DecryptPassword("not-valid-base64!!!"));
    }

    [Theory]
    [InlineData("short")]
    [InlineData("a longer password with spaces and symbols !@#$%")]
    [InlineData("unicode: café naïve résumé")]
    public void RoundTrip_VariousPasswords(string password)
    {
        var encrypted = ConfigManager.EncryptPassword(password);
        Assert.Equal(password, ConfigManager.DecryptPassword(encrypted));
    }
}
