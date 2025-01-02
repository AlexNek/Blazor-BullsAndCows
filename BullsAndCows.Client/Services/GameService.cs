using System.Timers;

using BullsAndCows.Client.Models;

namespace BullsAndCows.Client.Services
{
    public class GameService : IDisposable
    {
        private const int MAX_ATTEMPTS = 10;

        private const int MAX_GAME_DURATION_MINUTES = 60;

        public event Action OnTimerTick;

        private readonly IStatisticsService _statisticsService;

        private readonly ITimerWrapper _timer;

        private GameState _gameState;

        public bool IsTimerRunning { get; private set; }

        public GameService(IStatisticsService statisticsService, ITimerWrapper timer)
        {
            _statisticsService = statisticsService;
            //_timer = new System.Timers.Timer(1000); // 1-second interval
            _timer = timer;
            _timer.Elapsed += OnTimerElapsed;
            StartNewGame();
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        public string GetFormattedElapsedTime()
        {
            return $"{(int)_gameState.ElapsedTime.TotalMinutes}m {_gameState.ElapsedTime.Seconds}s";
        }

        public GameState GetGameState() => _gameState;

        public Guess? MakeGuess(string guess)
        {
            if (_gameState.IsGameOver)
            {
                return null;
            }

            if (!IsTimerRunning)
            {
                return null;
            }

            // Increment step count for each guess
            _gameState.StepCount++;

            string secret = _gameState.SecretNumber;
            int bulls = 0, cows = 0;

            if (guess.Length != secret.Length)
            {
                return null;
            }

            for (int i = 0; i < guess.Length; i++)
            {
                if (guess[i] == secret[i])
                {
                    bulls++;
                }
                else if (secret.Contains(guess[i]))
                {
                    cows++;
                }
            }

            var newGuess = new Guess { Value = guess, Bulls = bulls, Cows = cows };
            _gameState.Guesses.Add(newGuess);
            _gameState.AttemptsLeft--;

            if (bulls == 4)
            {
                EndGame(true);
                newGuess.IsCorrect = true;
            }
            else if (_gameState.AttemptsLeft == 0)
            {
                EndGame(false);
            }

            return newGuess;
        }

        public void StartNewGame(string? secretNumber = null)
        {
            _gameState = new GameState
                             {
                                 SecretNumber =
                                     secretNumber
                                     ?? GenerateSecretNumber(), // Use provided secret number or generate a new one
                                 Guesses = new List<Guess>(),
                                 IsGameOver = false,
                                 HasWon = false,
                                 AttemptsLeft = MAX_ATTEMPTS,
                                 StartTime = DateTime.Now,
                                 ElapsedTime = TimeSpan.Zero
                             };
        }

        public void StartTimer()
        {
            if (!IsTimerRunning)
            {
                IsTimerRunning = true;
                _gameState.StartTime = DateTime.Now;
                _timer.Start();
            }
        }

        public void StopTimer()
        {
            if (IsTimerRunning)
            {
                IsTimerRunning = false;
                _timer.Stop();
            }
        }

        private void EndGame(bool hasWon)
        {
            _gameState.IsGameOver = true;
            _gameState.HasWon = hasWon;
            StopTimer();

            // Don't store results with time above 1 hour
            if (_gameState.ElapsedTime.TotalMinutes >= MAX_GAME_DURATION_MINUTES)
            {
                return;
            }

            // Record the result
            var result = new GameResult
                             {
                                 Steps = _gameState.StepCount,
                                 Time = _gameState.ElapsedTime,
                                 HasWon = hasWon,
                                 LocalTime = DateTime.Now
                             };

            _statisticsService.AddResultAsync(result);
        }

        private string GenerateSecretNumber()
        {
            var digits = Enumerable.Range(0, 10).Select(x => x.ToString()).ToList();
            var firstDigit = digits[new Random().Next(1, 10)];
            digits.Remove(firstDigit);
            return firstDigit + string.Concat(digits.OrderBy(_ => Guid.NewGuid()).Take(3));
        }

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (IsTimerRunning)
            {
                _gameState.ElapsedTime = DateTime.Now - _gameState.StartTime;
                if (_gameState.ElapsedTime.TotalMinutes >= MAX_GAME_DURATION_MINUTES)
                {
                    EndGame(false);
                }

                OnTimerTick?.Invoke();
            }
        }
    }
}
