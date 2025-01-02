using BullsAndCows.Client.Models;

namespace BullsAndCows.Client.Services;

public interface IStatisticsService
{
    /// <summary>
    /// Occurs when statistics changed
    /// </summary>
    event Action OnStatisticsChanged;

    Task AddResultAsync(GameResult result);

    IEnumerable<GameResult> GetBestResults(int count);

    Task InitializeAsync();
}
