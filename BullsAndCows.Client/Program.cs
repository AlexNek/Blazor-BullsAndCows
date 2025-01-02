using BullsAndCows.Client.Services;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace BullsAndCows.Client
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.Services.AddSingleton<ITimerWrapper>(provider => new TimerWrapper(1000));
            builder.Services.AddScoped<IStatisticsService, StatisticsServiceClient>();
            builder.Services.AddScoped<GameService>();
            await builder.Build().RunAsync();
        }
    }
}
