using System.Diagnostics;

namespace HassLink.Sensors;

public class RamSensor : ISensor
{
    private readonly PerformanceCounter _availableBytes;

    public string Id => "ram";
    public string Name => "RAM Usage";
    public bool IsAvailable => true;

    public RamSensor()
    {
        _availableBytes = new PerformanceCounter("Memory", "Available Bytes");
    }

    public Task<IReadOnlyList<SensorReading>> GetReadingsAsync()
    {
        var totalBytes = (double)GetTotalPhysicalMemory();
        var availableBytes = (double)_availableBytes.NextValue();
        var usedBytes = totalBytes - availableBytes;
        var usedPercent = totalBytes > 0 ? Math.Round(usedBytes / totalBytes * 100.0, 1) : 0;
        var usedGb = Math.Round(usedBytes / SensorUnits.BytesPerGb, 2);
        var totalGb = Math.Round(totalBytes / SensorUnits.BytesPerGb, 2);

        IReadOnlyList<SensorReading> readings =
        [
            new("ram_percent", "RAM Usage", usedPercent.ToString("F1"), "%", null, "mdi:memory"),
            new("ram_used_gb", "RAM Used", usedGb.ToString("F2"), "GB", null, "mdi:memory"),
            new("ram_total_gb", "RAM Total", totalGb.ToString("F2"), "GB", null, "mdi:memory"),
        ];
        return Task.FromResult(readings);
    }

    private static long GetTotalPhysicalMemory()
    {
        // Use WMI via ComputerInfo equivalent — PerformanceCounter doesn't expose total RAM
        // Instead query via the kernel32 GlobalMemoryStatusEx
        var memStatus = new NativeMethods.MemoryStatusEx();
        memStatus.Init();
        if (NativeMethods.GlobalMemoryStatusEx(ref memStatus))
            return (long)memStatus.ullTotalPhys;
        return 0;
    }

    public void Dispose() => _availableBytes.Dispose();
}

internal static class NativeMethods
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct MemoryStatusEx
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

        public void Init() => dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(MemoryStatusEx));
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    public static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);
}
