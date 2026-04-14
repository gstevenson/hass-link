using System.Diagnostics;
using System.Net.NetworkInformation;

namespace HassLink.Sensors;

public class NetworkSensor : ISensor
{
    private readonly List<(string InstanceName, string AdapterName, PerformanceCounter Recv, PerformanceCounter Send)> _adapters = [];

    public string Id => "network";
    public string Name => "Network Throughput";
    public bool IsAvailable => _adapters.Count > 0;

    public NetworkSensor()
    {
        try
        {
            var category = new PerformanceCounterCategory("Network Interface");
            var instances = category.GetInstanceNames();

            foreach (var instance in instances)
            {
                // Skip loopback and virtual adapters
                if (instance.Contains("Loopback", StringComparison.OrdinalIgnoreCase) ||
                    instance.Contains("WAN Miniport", StringComparison.OrdinalIgnoreCase) ||
                    instance.Contains("Teredo", StringComparison.OrdinalIgnoreCase))
                    continue;

                var recv = new PerformanceCounter("Network Interface", "Bytes Received/sec", instance);
                var send = new PerformanceCounter("Network Interface", "Bytes Sent/sec", instance);

                // Prime counters
                recv.NextValue();
                send.NextValue();

                _adapters.Add((instance, SanitiseInstanceName(instance), recv, send));
            }
        }
        catch
        {
            // If performance counters aren't available, skip silently
        }
    }

    public Task<IReadOnlyList<SensorReading>> GetReadingsAsync()
    {
        var readings = new List<SensorReading>();

        foreach (var (instance, name, recv, send) in _adapters)
        {
            var recvMbps = Math.Round(recv.NextValue() / (1024 * 1024), 3);
            var sendMbps = Math.Round(send.NextValue() / (1024 * 1024), 3);
            var safeId = MakeId(instance);

            readings.Add(new($"net_{safeId}_recv", $"Network {name} Download", recvMbps.ToString("F3"), "MB/s", null, "mdi:download-network"));
            readings.Add(new($"net_{safeId}_send", $"Network {name} Upload", sendMbps.ToString("F3"), "MB/s", null, "mdi:upload-network"));
        }

        return Task.FromResult<IReadOnlyList<SensorReading>>(readings);
    }

    private static string SanitiseInstanceName(string name)
    {
        // Shorten long adapter names for display
        if (name.Length > 30)
            name = name[..30];
        return name;
    }

    private static string MakeId(string instanceName)
    {
        // Make a safe ID from the adapter instance name
        var safe = new System.Text.StringBuilder();
        foreach (var c in instanceName.ToLower())
        {
            if (char.IsLetterOrDigit(c))
                safe.Append(c);
            else
                safe.Append('_');
        }
        return safe.ToString().Trim('_');
    }

    public void Dispose()
    {
        foreach (var (_, _, recv, send) in _adapters)
        {
            recv.Dispose();
            send.Dispose();
        }
    }
}
