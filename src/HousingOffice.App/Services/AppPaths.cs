using System;
using System.IO;

namespace HousingOffice.Services;

public static class AppPaths
{
    public static string DataDir { get; } = InitDataDir();
    public static string HistoryDir => Path.Combine(DataDir, "history");

    private static string InitDataDir()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "HousingOffice");
        Directory.CreateDirectory(dir);
        Directory.CreateDirectory(Path.Combine(dir, "history"));
        return dir;
    }

    public static string DbPathForYear(int year) => Path.Combine(DataDir, $"data_{year}.db");
}
