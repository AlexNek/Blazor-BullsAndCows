using System.Text.Json;

using BullsAndCows.Client.Models;

using Microsoft.JSInterop;

namespace BullsAndCows.Client.Services
{
    public class StatisticsServiceClient : IStatisticsService
    {
        private const string LocalStorageKey = "GameResults";

        private const int MaxStoredResults = 30; // Store only top 30 results

        public event Action OnStatisticsChanged; // Implementation of the event

        private List<GameResult> _results = new List<GameResult>();

        public IJSRuntime JsRuntime { get; }

        public StatisticsServiceClient(IJSRuntime jsRuntime)
        {
            JsRuntime = jsRuntime;
        }

        public async Task AddResultAsync(GameResult result)
        {
            var oldCount = _results.Count;
            if (result.HasWon)
            {
                _results.Add(result);
                _results = _results
                    .OrderBy(r => r.Time)
                    .ThenBy(r => r.Steps)
                    .Take(MaxStoredResults)
                    .ToList();

                await SaveResultsAsync();
            }

            if (_results.Count != oldCount || _results.Contains(result))
            {
                NotifyStatisticsChanged();
            }
        }


        public IEnumerable<GameResult> GetBestResults(int count)
        {
            return _results
                .Take(Math.Min(count, MaxStoredResults));
        }

        public async Task InitializeAsync()
        {
            await LoadResultsAsync();
            NotifyStatisticsChanged();
        }

        private async Task LoadResultsAsync()
        {
            var json = await JsRuntime.InvokeAsync<string>(
                           "localStorage.getItem",
                           LocalStorageKey);

            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            try
            {
                var loadedResults = JsonSerializer.Deserialize<List<GameResult>>(json);
                if (loadedResults != null)
                {
                    _results = loadedResults
                        .OrderBy(r => r.Time) // Prioritize faster times
                        .ThenBy(r => r.Steps) // Within similar times, prioritize fewer steps
                        .Take(MaxStoredResults)
                        .ToList();
                    ;
                }
            }
            catch (JsonException)
            {
                // Handle potential deserialization errors
                // Optionally log the error or notify the user
            }
        }

        private void NotifyStatisticsChanged()
        {
            OnStatisticsChanged?.Invoke();
        }

        private async Task SaveResultsAsync()
        {
            var json = JsonSerializer.Serialize(_results);
            await JsRuntime.InvokeVoidAsync("localStorage.setItem", LocalStorageKey, json);
        }
    }
}
