using System.Timers;

namespace BullsAndCows.Client.Services;

public interface ITimerWrapper : IDisposable, IAsyncDisposable
{
    event ElapsedEventHandler Elapsed;

    void Start();

    void Stop();
}
