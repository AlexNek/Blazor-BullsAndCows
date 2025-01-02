namespace BullsAndCows.Client.Models;

public class Guess
{
    public int Bulls { get; set; }

    public int Cows { get; set; }

    public string Value { get; set; }

    public bool IsCorrect { get; set; }
}
