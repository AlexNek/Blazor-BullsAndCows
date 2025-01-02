using BullsAndCows.Client.Models;
using BullsAndCows.Client.Services;

namespace BullsAndCows.Services;

public class StatisticsServiceServer : IStatisticsService
{
    public event Action? OnStatisticsChanged;

    private readonly List<GameResult> _results = Enumerable.Empty<GameResult>().ToList();

    public async Task AddResultAsync(GameResult result)
    {
        await Task.Delay(0);
        _results.Add(result);
    }

    public IEnumerable<GameResult> GetBestResults(int count)
    {
        return _results
            .OrderBy(r => r.Steps) // Sort by steps taken (ascending)
            .ThenBy(r => r.Time) // Then by time taken (ascending)
            .Take(count); // Get top 'count' results
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }
}
