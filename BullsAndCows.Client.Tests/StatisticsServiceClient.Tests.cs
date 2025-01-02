using BullsAndCows.Client.Models;
using BullsAndCows.Client.Services;

using FluentAssertions;

using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

using Moq;

namespace BullsAndCows.Client.Tests;

public class StatisticsServiceClientTests
{
    [Fact]
    public async Task AddResultAsync_ShouldAddResultAndNotifyChange()
    {
        // Arrange
        var cut = CreateClassUnderTest();
        var notificationReceived = false;
        cut.OnStatisticsChanged += () => notificationReceived = true;

        // Act
        await cut.AddResultAsync(
            new GameResult { Steps = 5, Time = TimeSpan.FromMinutes(1), HasWon = true });

        // Assert
        notificationReceived.Should().BeTrue();
        cut.GetBestResults(1).Should().HaveCount(1);
        //Mock.Get(cut.JsRuntime).Verify(
        //    js => js.InvokeVoidAsync("localStorage.setItem", It.IsAny<object[]>()),
        //    Times.Once);
        Mock.Get(cut.JsRuntime).Verify(
            js => js.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.IsAny<object[]>()),
            Times.Once);
    }

    [Fact]
    public async Task AddResultAsync_ShouldCorrectlyOrderResultsWithSameTimeAndSteps()
    {
        // Arrange
        var cut = CreateClassUnderTest();
        var result1 = new GameResult { Steps = 5, Time = TimeSpan.FromMinutes(1), HasWon = true };
        var result2 = new GameResult { Steps = 5, Time = TimeSpan.FromMinutes(2), HasWon = true };
        var result3 = new GameResult { Steps = 5, Time = TimeSpan.FromMinutes(3), HasWon = true };

        // Act
        await cut.AddResultAsync(result1);
        await cut.AddResultAsync(result2);
        await cut.AddResultAsync(result3);

        // Assert
        var results = cut.GetBestResults(3).ToList();
        results.Should().HaveCount(3);
        results[0].Should().BeEquivalentTo(result1);
        results[1].Should().BeEquivalentTo(result2);
        results[2].Should().BeEquivalentTo(result3);
    }

    [Fact]
    public async Task AddResultAsync_ShouldHandleAddingResultWhenListIsAtMaxCapacity()
    {
        // Arrange
        var cut = CreateClassUnderTest();
        for (int i = 0; i < 30; i++)
        {
            await cut.AddResultAsync(new GameResult { Steps = i, Time = TimeSpan.FromMinutes(i+1), HasWon = true });
        }

        var newResult = new GameResult { Steps = 1, Time = TimeSpan.FromSeconds(30), HasWon = true };

        // Act
        await cut.AddResultAsync(newResult);

        // Assert
        var results = cut.GetBestResults(31).ToList();
        results.Should().HaveCount(30);
        results[0].Should().BeEquivalentTo(newResult);
    }

    [Fact]
    public async Task AddResultAsync_ShouldHandleAddingResultWithMaximumPossibleTimeAndSteps()
    {
        // Arrange
        var cut = CreateClassUnderTest();
        var maxResult = new GameResult { Steps = int.MaxValue, Time = TimeSpan.MaxValue, HasWon = true };

        // Act
        await cut.AddResultAsync(maxResult);

        // Assert
        var results = cut.GetBestResults(1).ToList();
        results.Should().HaveCount(1);
        results[0].Should().BeEquivalentTo(maxResult);
        Mock.Get(cut.JsRuntime).Verify(
            js => js.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.IsAny<object[]>()),
            Times.Once);
    }

    [Fact]
    public async Task AddResultAsync_ShouldLimitStoredResults()
    {
        // Arrange

        var cut = CreateClassUnderTest();

        // Act
        for (int i = 0; i < 35; i++)
        {
            await cut.AddResultAsync(new GameResult { Steps = i, Time = TimeSpan.FromMinutes(i), HasWon = true });
        }

        // Assert
        cut.GetBestResults(100).Should().HaveCount(30);
    }

    [Fact]
    public async Task AddResultAsync_ShouldMaintainOrderWhenAddingResultWithSameStepsButFasterTime()
    {
        // Arrange
        var cut = CreateClassUnderTest();
        var initialResult = new GameResult { Steps = 5, Time = TimeSpan.FromMinutes(2), HasWon = true };
        var newResult = new GameResult { Steps = 5, Time = TimeSpan.FromMinutes(1), HasWon = true };

        await cut.AddResultAsync(initialResult);

        // Act
        await cut.AddResultAsync(newResult);

        // Assert
        var results = cut.GetBestResults(2).ToList();
        results.Should().HaveCount(2);
        results[0].Should().BeEquivalentTo(newResult);
        results[1].Should().BeEquivalentTo(initialResult);
    }

    [Fact]
    public async Task AddResultAsync_ShouldMaintainOrderWhenAddingResultWithSameTimeButFewerSteps()
    {
        // Arrange
        var cut = CreateClassUnderTest();
        var initialResult = new GameResult { Steps = 5, Time = TimeSpan.FromMinutes(1), HasWon = true };
        var newResult = new GameResult { Steps = 4, Time = TimeSpan.FromMinutes(1), HasWon = true };

        await cut.AddResultAsync(initialResult);

        // Act
        await cut.AddResultAsync(newResult);

        // Assert
        var results = cut.GetBestResults(2).ToList();
        results.Should().HaveCount(2);
        results[0].Should().BeEquivalentTo(newResult);
        results[1].Should().BeEquivalentTo(initialResult);
    }

    [Fact]
    public async Task AddResultAsync_ShouldNotNotifyChangeIfNoNewResultIsAddedDueToCapacityLimit()
    {
        // Arrange
        var cut = CreateClassUnderTest();
        var notificationReceived = false;
        for (int i = 0; i < 30; i++)
        {
            await cut.AddResultAsync(new GameResult { Steps = i, Time = TimeSpan.FromMinutes(i+1), HasWon = true });
        }

        var newResult = new GameResult { Steps = 31, Time = TimeSpan.FromMinutes(32), HasWon = true };
        // Act
        cut.OnStatisticsChanged += () => notificationReceived = true;
        await cut.AddResultAsync(newResult);

        // Assert
        notificationReceived.Should().BeFalse();
        cut.GetBestResults(31).Should().NotContain(newResult);
    }

    [Fact]
    public async Task AddResultAsync_ShouldNotifyChangeIfNewWinningResultIsAddedDueToCapacityLimit()
    {
        // Arrange
        var cut = CreateClassUnderTest();
        var notificationReceived = false;
       

        for (int i = 0; i < 30; i++)
        {
            await cut.AddResultAsync(new GameResult { HasWon = true, Steps = i, Time = TimeSpan.FromMinutes(i + 1) });
        }

        notificationReceived = false; // Reset the flag

        var newResult = new GameResult { HasWon = true, Steps = 31, Time = TimeSpan.FromMinutes(1) };

        // Act
        cut.OnStatisticsChanged += () => notificationReceived = true;
        await cut.AddResultAsync(newResult);

        // Assert
        notificationReceived.Should().BeTrue(); // It should still notify, as we're adding a winning result
        cut.GetBestResults(31).Should().Contain(newResult);
    }

    [Fact]
    public async Task GetBestResults_ShouldReturnOrderedResults()
    {
        // Arrange

        var cut = CreateClassUnderTest();
        await cut.AddResultAsync(new GameResult { Steps = 5, Time = TimeSpan.FromMinutes(2), HasWon = true });
        await cut.AddResultAsync(new GameResult { Steps = 4, Time = TimeSpan.FromMinutes(1), HasWon = true });
        await cut.AddResultAsync(new GameResult { Steps = 6, Time = TimeSpan.FromMinutes(3), HasWon = true });

        // Act
        var results = cut.GetBestResults(3).ToList();

        // Assert
        results.Should().HaveCount(3);
        results[0].Time.Should().Be(TimeSpan.FromMinutes(1));
        results[1].Time.Should().Be(TimeSpan.FromMinutes(2));
        results[2].Time.Should().Be(TimeSpan.FromMinutes(3));
    }

    [Fact]
    public async Task InitializeAsync_ShouldLoadResultsAndNotifyChange()
    {
        // Arrange
        var cut = CreateClassUnderTest("[{\"Steps\":5,\"Time\":\"00:01:00\"}]");
        var notificationReceived = false;
        cut.OnStatisticsChanged += () => notificationReceived = true;

        // Act
        await cut.InitializeAsync();

        // Assert
        notificationReceived.Should().BeTrue();
        cut.GetBestResults(1).Should().HaveCount(1);

        // If we need to verify JSRuntime calls:
        // Mock.Get(cut.JsRuntime).Verify(...);
    }

    private StatisticsServiceClient CreateClassUnderTest(string initialStorageContent = null)
    {
        var mockJsRuntime = new Mock<IJSRuntime>();
        if (initialStorageContent != null)
        {
            mockJsRuntime.Setup(
                    js => js.InvokeAsync<string>(It.IsAny<string>(), It.IsAny<object[]>()))
                .ReturnsAsync(initialStorageContent);
        }

        return new StatisticsServiceClient(mockJsRuntime.Object);
    }
}
