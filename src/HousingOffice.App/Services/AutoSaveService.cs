using System;
using System.Threading;
using System.Windows.Threading;

namespace HousingOffice.Services;

public sealed class AutoSaveService : IDisposable
{
    private readonly DispatcherTimer _debounce;
    private readonly DispatcherTimer _heartbeat;
    private readonly Action _flush;
    public event Action<DateTime>? Saved;

    public AutoSaveService(Action flush, TimeSpan? debounce = null, TimeSpan? heartbeat = null)
    {
        _flush = flush;
        _debounce = new DispatcherTimer { Interval = debounce ?? TimeSpan.FromSeconds(2) };
        _debounce.Tick += (_, _) => { _debounce.Stop(); DoFlush(); };
        _heartbeat = new DispatcherTimer { Interval = heartbeat ?? TimeSpan.FromMinutes(5) };
        _heartbeat.Tick += (_, _) => DoFlush();
        _heartbeat.Start();
    }

    public void MarkDirty()
    {
        _debounce.Stop();
        _debounce.Start();
    }

    public void FlushNow() => DoFlush();

    private void DoFlush()
    {
        try
        {
            _flush();
            Saved?.Invoke(DateTime.Now);
        }
        catch { }
    }

    public void Dispose()
    {
        _debounce.Stop();
        _heartbeat.Stop();
    }
}
