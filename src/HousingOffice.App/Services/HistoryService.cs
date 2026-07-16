using System;
using System.IO;
using System.Linq;

namespace HousingOffice.Services;

public sealed class HistoryService
{
    private readonly string _historyDir;
    private readonly TimeSpan _retention;

    public HistoryService(string dataDir, TimeSpan? retention = null)
    {
        _historyDir = Path.Combine(dataDir, "history");
        _retention = retention ?? TimeSpan.FromDays(2);
        Directory.CreateDirectory(_historyDir);
    }

    public string Snapshot(string dbPath)
    {
        var stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var name = Path.GetFileNameWithoutExtension(dbPath);
        var dest = Path.Combine(_historyDir, $"{name}__{stamp}.db");
        File.Copy(dbPath, dest, overwrite: true);
        Prune();
        return dest;
    }

    public int SnapshotCount(string dbPath)
    {
        var name = Path.GetFileNameWithoutExtension(dbPath);
        return Directory.EnumerateFiles(_historyDir, $"{name}__*.db").Count();
    }

    private void Prune()
    {
        var cutoff = DateTime.Now - _retention;
        foreach (var file in Directory.EnumerateFiles(_historyDir, "*.db"))
        {
            try
            {
                if (File.GetLastWriteTime(file) < cutoff) File.Delete(file);
            }
            catch { }
        }
    }
}
