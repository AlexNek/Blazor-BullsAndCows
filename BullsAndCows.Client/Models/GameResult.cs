namespace BullsAndCows.Client.Models;

public class GameResult
{
    public bool HasWon { get; set; }

    public DateTime? LocalTime { get; set; }

    public int Steps { get; set; }

    public TimeSpan Time { get; set; }
}
