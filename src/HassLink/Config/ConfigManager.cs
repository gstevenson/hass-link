using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HassLink.Config;

public static class ConfigManager
{
    private static readonly string ConfigDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "hass-link");

    public static string ConfigDirectory => ConfigDir;

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public static AppConfig Load(string? configPath = null, Action? onParseError = null)
    {
        var path = configPath ?? ConfigPath;
        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
                config.Mqtt.Password = DecryptPassword(config.Mqtt.EncryptedPassword);
                return config;
            }
        }
        catch (Exception)
        {
            (onParseError ?? DefaultParseError)();
        }
        return new AppConfig();
    }

    private static void DefaultParseError() =>
        MessageBox.Show(
            "Configuration file could not be loaded. Default settings will be used.",
            "hass-link",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);

    public static void Save(AppConfig config, string? configPath = null)
    {
        var path = configPath ?? ConfigPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        config.Mqtt.EncryptedPassword = EncryptPassword(config.Mqtt.Password);
        var json = JsonSerializer.Serialize(config, JsonOptions);

        // Write atomically: write to a temp file, then replace, so a crash mid-write
        // can never leave config.json empty or partially written.
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, json);
        if (File.Exists(path))
            File.Replace(tmp, path, path + ".bak");
        else
            File.Move(tmp, path);
    }

    internal static string EncryptPassword(string password)
    {
        if (string.IsNullOrEmpty(password)) return "";
        var bytes = Encoding.UTF8.GetBytes(password);
        var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encrypted);
    }

    internal static string DecryptPassword(string encryptedPassword)
    {
        if (string.IsNullOrEmpty(encryptedPassword)) return "";
        try
        {
            var bytes = Convert.FromBase64String(encryptedPassword);
            var decrypted = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            return "";
        }
    }
}
