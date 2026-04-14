using System.Text;

namespace HassLink.Sensors;

public class ActiveWindowSensor : ISensor
{
    public string Id => "activeWindow";
    public string Name => "Active Window";
    public bool IsAvailable => true;

    public Task<IReadOnlyList<SensorReading>> GetReadingsAsync()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        var title = "";
        if (hwnd != IntPtr.Zero)
        {
            var sb = new StringBuilder(256);
            if (NativeMethods.GetWindowText(hwnd, sb, sb.Capacity) > 0)
                title = sb.ToString();
        }

        IReadOnlyList<SensorReading> readings =
        [
            new("active_window", "Active Window", title, null, null, "mdi:application")
        ];
        return Task.FromResult(readings);
    }

    public void Dispose() { }
}
