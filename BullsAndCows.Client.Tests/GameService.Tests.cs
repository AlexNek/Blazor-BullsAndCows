using System.Timers;

using BullsAndCows.Client.Models;
using BullsAndCows.Client.Services;

using FluentAssertions;

using Moq;

namespace BullsAndCows.Client.Tests
{
    public class GameServiceTests : IDisposable
    {
        private readonly Mock<IStatisticsService> _mockStatisticsService;

        private readonly Mock<ITimerWrapper> _mockTimer;

        public GameServiceTests()
        {
            _mockStatisticsService = new Mock<IStatisticsService>();
            _mockTimer = new Mock<ITimerWrapper>();
        }

        public void Dispose()
        {
            // Clean up any resources if necessary
        }

        [Fact]
        public void EndGame_ShouldNotifyStatisticsServiceOnWin()
        {
            // Arrange
            var cut = CreateClassUnderTest();
            cut.StartNewGame();
            cut.StartTimer();

            var secretNumber = cut.GetGameState().SecretNumber;

            // Set up a variable to capture the result
            GameResult capturedResult = null;

            // Set up the mock to capture the result when AddResultAsync is called
            _mockStatisticsService
                .Setup(s => s.AddResultAsync(It.IsAny<GameResult>()))
                .Callback<GameResult>(result => capturedResult = result);

            // Act: Make correct guesses to win the game
            cut.MakeGuess(secretNumber);

            // Assert: Check if statistics service was called with a result
            _mockStatisticsService.Verify(
                s => s.AddResultAsync(It.IsAny<GameResult>()),
                Times.Once);

            // Optionally assert on captured result properties if needed
            capturedResult.Should().NotBeNull();
            capturedResult.HasWon.Should().BeTrue();
        }

        [Fact]
        public void MakeGuess_ShouldAddGuessAndUpdateState()
        {
            // Arrange
            var cut = CreateClassUnderTest();
            cut.StartNewGame();
            cut.StartTimer();
            var secretNumber = cut.GetGameState().SecretNumber;

            // Act
            var guessResult = cut.MakeGuess(secretNumber);

            // Assert
            guessResult.Should().NotBeNull();
            guessResult.Value.Should().Be(secretNumber);
            guessResult.Bulls.Should().Be(4); // Assuming the guess is correct
            guessResult.Cows.Should().Be(0);
            cut.GetGameState().Guesses.Should().ContainSingle();
        }

        [Fact]
        public void MakeGuess_ShouldCorrectlyCalculateBullsAndCows_WhenAllDigitsInWrongPositions()
        {
            // Arrange
            // Set a secret number where all digits are in the guess but in wrong positions
            var secretNumber = "1234";
            var guess = "4321";
            var cut = CreateClassUnderTest();
            cut.StartNewGame(secretNumber);
            cut.StartTimer();

            // Act
            var guessResult = cut.MakeGuess(guess);

            // Assert
            guessResult.Should().NotBeNull();
            guessResult.Bulls.Should().Be(0);
            guessResult.Cows.Should().Be(4);
        }

        [Fact]
        public void MakeGuess_ShouldCorrectlyCalculateBullsAndCows_WhenNoMatchingDigits()
        {
            // Arrange
            // Set a secret number that does not share any digits with the guess
            var secretNumber = "1234";
            var guess = "5678";

            var cut = CreateClassUnderTest();
            cut.StartNewGame(secretNumber);
            cut.StartTimer();

            // Act
            var guessResult = cut.MakeGuess(guess);

            // Assert
            guessResult.Should().NotBeNull();
            guessResult.Bulls.Should().Be(0);
            guessResult.Cows.Should().Be(0);
        }

        [Fact]
        public void
            MakeGuess_ShouldCorrectlyCalculateBullsAndCows_WhenSomeDigitsInCorrectAndWrongPositions()
        {
            // Arrange
            // Set a secret number with some digits in correct positions and some in wrong positions
            var secretNumber = "1234";
            var guess = "1243";

            var cut = CreateClassUnderTest();
            cut.StartNewGame(secretNumber);
            cut.StartTimer();

            // Act
            var guessResult = cut.MakeGuess(guess);

            // Assert
            guessResult.Should().NotBeNull();
            guessResult.Bulls.Should().Be(2); // '1' and '2' are in correct positions
            guessResult.Cows.Should().Be(2); // '3' and '4' are in wrong positions
        }

        [Fact]
        public void MakeGuess_ShouldDecrementAttemptsLeftAfterEachGuess()
        {
            // Arrange
            var cut = CreateClassUnderTest();
            cut.StartNewGame();
            cut.StartTimer();
            var initialAttemptsLeft = cut.GetGameState().AttemptsLeft;
            var guess = "5678"; // Any guess

            // Act
            cut.MakeGuess(guess);

            // Assert
            cut.GetGameState().AttemptsLeft.Should().Be(initialAttemptsLeft - 1);
        }

        [Fact]
        public void MakeGuess_ShouldEndGameWhenCorrectGuess()
        {
            // Arrange
            var cut = CreateClassUnderTest();
            cut.StartNewGame();
            cut.StartTimer();
            var secretNumber = cut.GetGameState().SecretNumber;

            // Act
            var guessResult = cut.MakeGuess(secretNumber);

            // Assert
            guessResult.IsCorrect.Should().BeTrue();
            cut.GetGameState().IsGameOver.Should().BeTrue();
        }

        [Fact]
        public void MakeGuess_ShouldEndGameWithLossWhenAttemptsReachZeroAndGuessIsIncorrect()
        {
            // Arrange

            // Set a secret number that does not match the guess
            var secretNumber = "1234";
            var incorrectGuess = "5678";
            var cut = CreateClassUnderTest();
            cut.StartNewGame(secretNumber);
            cut.StartTimer();

            // Act: Make 9 incorrect guesses
            for (int i = 0; i < 9; i++)
            {
                cut.MakeGuess(incorrectGuess); // Make an incorrect guess
            }

            // The last guess should result in the game ending with a loss
            var guessResult = cut.MakeGuess(incorrectGuess);

            // Assert
            guessResult.Should().NotBeNull();
            guessResult.IsCorrect.Should().BeFalse();
            cut.GetGameState().IsGameOver.Should().BeTrue();
            cut.GetGameState().HasWon.Should().BeFalse();
        }

        [Fact]
        public void MakeGuess_ShouldHandleShortGuessesWithoutError()
        {
            // Arrange
            var cut = CreateClassUnderTest();
            cut.StartNewGame("1234");
            cut.StartTimer();

            var shortGuess = "12"; // Fewer digits than the secret number

            // Act
            var guessResult = cut.MakeGuess(shortGuess);

            // Assert
            guessResult.Should().BeNull();
            //guessResult.Bulls.Should().Be(0); // No bulls since the guess is shorter
            //guessResult.Cows.Should().Be(0); // No cows since the guess is shorter
            cut.GetGameState().Guesses.Should().BeEmpty();
        }

        [Fact]
        public void MakeGuess_ShouldNotAddGuessIfGameIsOver()
        {
            // Arrange
            var cut = CreateClassUnderTest();
            cut.StartNewGame("1234"); // Start a new game with a known secret number
            cut.StartTimer();

            // Simulate making incorrect guesses until attempts reach zero
            for (int i = 0; i < 10; i++)
            {
                cut.MakeGuess("5678"); // Make an incorrect guess
            }

            // Now the game should be over, so any further guesses should not be added
            var initialGuessCount = cut.GetGameState().Guesses.Count;

            // Act: Attempt to make another guess while the game is over
            var guessResult = cut.MakeGuess("9999"); // Another incorrect guess

            // Assert
            guessResult.Should().BeNull(); // The result should be null since the game is over
            cut.GetGameState().Guesses.Count.Should()
                .Be(initialGuessCount); // The guess count should remain unchanged
        }

        [Fact]
        public void MakeGuess_ShouldNotModifyGameState_WhenGuessIsMadeAfterGameOver()
        {
            // Arrange
            var cut = CreateClassUnderTest();
            cut.StartNewGame("1234"); // Start a new game with a known secret number
            cut.StartTimer();

            // Simulate making incorrect guesses until attempts reach zero
            for (int i = 0; i < 10; i++)
            {
                cut.MakeGuess("5678"); // Make an incorrect guess
            }

            var initialAttemptsLeft = cut.GetGameState().AttemptsLeft;
            var initialGuessCount = cut.GetGameState().Guesses.Count;

            // Act: Attempt to make another guess while the game is over
            var guessResult = cut.MakeGuess("9999"); // Another incorrect guess

            // Assert
            guessResult.Should().BeNull(); // The result should be null since the game is over
            cut.GetGameState().AttemptsLeft.Should()
                .Be(initialAttemptsLeft); // The attempts left should remain unchanged
            cut.GetGameState().Guesses.Count.Should()
                .Be(initialGuessCount); // The guess count should remain unchanged
        }

        [Fact]
        public void StartNewGame_ShouldInitializeGameState()
        {
            // Arrange
            var cut = CreateClassUnderTest();

            // Act
            cut.StartNewGame();

            // Assert
            var gameState = cut.GetGameState();
            gameState.Should().NotBeNull();
            gameState.IsGameOver.Should().BeFalse();
            gameState.AttemptsLeft.Should().Be(10); // MAX_ATTEMPTS
            gameState.Guesses.Should().BeEmpty();
            gameState.SecretNumber.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Timer_ShouldUpdateElapsedTime()
        {
            // Arrange
            var cut = CreateClassUnderTest();
            cut.StartNewGame();
            cut.StartTimer();

            // Act: Simulate timer elapsed event
            // Here we create a DateTime for testing purposes
            var signalTime = DateTime.Now; // This would represent when the event occurred
            var elapsedEventArgs =
                new ElapsedEventArgs(signalTime); // Create an instance of ElapsedEventArgs

            // Raise the event with the correct arguments
            _mockTimer.Raise(t => t.Elapsed += null, elapsedEventArgs);

            // Assert: Check that elapsed time is updated
            var gameState = cut.GetGameState();
            gameState.ElapsedTime.TotalSeconds.Should().BeGreaterThan(0);
        }

        private GameService CreateClassUnderTest()
        {
            return new GameService(_mockStatisticsService.Object, _mockTimer.Object);
        }
    }
}
