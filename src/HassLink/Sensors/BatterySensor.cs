namespace HassLink.Sensors;

public class BatterySensor : ISensor
{
    public string Id => "battery";
    public string Name => "Battery";
    public bool IsAvailable => SystemInformation.PowerStatus.BatteryChargeStatus != BatteryChargeStatus.NoSystemBattery
                             && SystemInformation.PowerStatus.BatteryChargeStatus != BatteryChargeStatus.Unknown;

    public Task<IReadOnlyList<SensorReading>> GetReadingsAsync()
    {
        var status = SystemInformation.PowerStatus;
        var percent = Math.Round(status.BatteryLifePercent * 100f, 1);
        var charging = (status.BatteryChargeStatus & BatteryChargeStatus.Charging) != 0;
        var pluggedIn = status.PowerLineStatus == PowerLineStatus.Online;

        IReadOnlyList<SensorReading> readings =
        [
            new("battery_percent", "Battery", percent.ToString("F1"), "%", "battery", "mdi:battery"),
            new("battery_charging", "Battery Charging", charging ? "True" : "False", null, null, "mdi:battery-charging"),
            new("battery_plugged_in", "AC Power", pluggedIn ? "True" : "False", null, null, "mdi:power-plug"),
        ];
        return Task.FromResult(readings);
    }

    public void Dispose() { }
}
