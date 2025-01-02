namespace BullsAndCows.Client.Models;

public class GameState
{
    public int AttemptsLeft { get; set; }

    public TimeSpan ElapsedTime { get; set; }

    public List<Guess> Guesses { get; set; }

    public bool HasWon { get; set; }

    public bool IsGameOver { get; set; }

    public string SecretNumber { get; set; }

    public DateTime StartTime { get; set; }

    public int StepCount { get; set; } = 0;
}
