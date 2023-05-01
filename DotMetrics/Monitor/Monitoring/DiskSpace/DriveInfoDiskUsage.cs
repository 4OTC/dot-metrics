using System;
using System.IO;

namespace DotMetrics.Monitor.Monitoring.DiskSpace;

public class DriveInfoDiskUsage : IDiskUsage
{
    private readonly DriveInfo _driveInfo;

    private DriveInfoDiskUsage(DriveInfo driveInfo)
    {
        _driveInfo = driveInfo;
    }

    public decimal GetUtilisationPercentage()
    {
        return CalculateUtilisation(_driveInfo);
    }

    public static IDiskUsage FromDriveInfo(string mountPoint)
    {
        return new DriveInfoDiskUsage(GetDriveInfo(mountPoint));
    }

    private static decimal CalculateUtilisation(DriveInfo driveInfo)
    {
        return new decimal(100 * ((driveInfo.TotalSize - driveInfo.AvailableFreeSpace) / (double)driveInfo.TotalSize));
    }

    private static DriveInfo GetDriveInfo(string mountPoint)
    {
        var driveInfos = DriveInfo.GetDrives();
        foreach (DriveInfo driveInfo in driveInfos)
        {
            if (driveInfo.VolumeLabel.Equals(mountPoint))
            {
                return driveInfo;
            }
        }

        throw new Exception($"Could not find DriveInfo with volume label {mountPoint}");
    }
}