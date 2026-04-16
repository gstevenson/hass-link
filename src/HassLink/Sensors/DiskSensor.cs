namespace HassLink.Sensors;

public class DiskSensor : ISensor
{
    public string Id => "disk";
    public string Name => "Disk Usage";
    public bool IsAvailable => true;

    public Task<IReadOnlyList<SensorReading>> GetReadingsAsync()
    {
        var readings = new List<SensorReading>();

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType != DriveType.Fixed || !drive.IsReady)
                continue;

            var driveLetter = drive.Name.TrimEnd('\\').TrimEnd(':').ToLower();
            var totalGb = Math.Round((double)drive.TotalSize / SensorUnits.BytesPerGb, 2);
            var freeGb = Math.Round((double)drive.AvailableFreeSpace / SensorUnits.BytesPerGb, 2);
            var usedGb = Math.Round(totalGb - freeGb, 2);
            var usedPercent = totalGb > 0 ? Math.Round(usedGb / totalGb * 100.0, 1) : 0;

            readings.Add(new($"disk_{driveLetter}_free", $"Disk {drive.Name} Free", freeGb.ToString("F2"), "GB", null, "mdi:harddisk"));
            readings.Add(new($"disk_{driveLetter}_used", $"Disk {drive.Name} Used", usedGb.ToString("F2"), "GB", null, "mdi:harddisk"));
            readings.Add(new($"disk_{driveLetter}_percent", $"Disk {drive.Name} Usage", usedPercent.ToString("F1"), "%", null, "mdi:harddisk"));
        }

        return Task.FromResult<IReadOnlyList<SensorReading>>(readings);
    }

    public void Dispose() { }
}
