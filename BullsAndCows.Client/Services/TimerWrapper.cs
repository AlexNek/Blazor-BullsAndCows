using System.Timers;

namespace BullsAndCows.Client.Services;

public class TimerWrapper : ITimerWrapper
{
    public event ElapsedEventHandler Elapsed
    {
        add => _timer.Elapsed += value;
        remove => _timer.Elapsed -= value;
    }

    private readonly System.Timers.Timer _timer;

    public TimerWrapper(double interval)
    {
        _timer = new System.Timers.Timer(interval);
    }

    public void Dispose() => _timer.Dispose();

    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();
}
