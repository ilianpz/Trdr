using System.CommandLine;
using Microsoft.Extensions.Logging;
using Trdr.App;
using Trdr.Async;
using Trdr.Connectivity.CoinJar;
using Trdr.Connectivity.CoinJar.Phoenix;

var loggerFactory = Application.SetupDefaultLogger();
var logger = loggerFactory.CreateLogger("Main");

TimeSpan until = TimeSpan.Zero;
IEnumerable<string> channels = Array.Empty<string>();

int result = ParseCommandLine();
if (result != 0)
    return result;

Connection connection;
try
{
    logger.LogInformation("Connecting...");
    connection = Connection.Create();
    await connection.Connect();
    logger.LogInformation("Connected");

    ReadChannels();
    await End();
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex}");
    logger.LogError(ex, "Error encountered");
    return 1;
}

int ParseCommandLine()
{
    var rootCmd = new RootCommand();
    var coinJarCmd = new Command("coinjar");
    var untilOption = new Option<TimeSpan>("--until");
    coinJarCmd.AddGlobalOption(untilOption);
    var channelOption = new Option<IEnumerable<string>>("--channels") { AllowMultipleArgumentsPerToken = true };
    coinJarCmd.AddOption(channelOption);
    coinJarCmd.SetHandler(
        (untilValue, channelsValue) =>
        {
            until = untilValue;
            channels = channelsValue;
        },
        untilOption, channelOption);
    rootCmd.Add(coinJarCmd);

    return rootCmd.Invoke(args);
}

void ReadChannels()
{
    foreach (var channel in channels)
    {
        Task.Run(async () =>
        {
            var channelLogger = loggerFactory.CreateLogger(channel);

            try
            {
                await foreach (MessagePair pair in connection.Subscribe(channel))
                {
                    channelLogger.LogInformation("{RawMessage}", pair.RawMessage);
                }
            }
            catch (Exception ex)
            {
                channelLogger.LogError(ex, "Error from channel");
            }
        }).Forget();
    }
}

async Task End()
{
    if (until > TimeSpan.Zero)
    {
        Console.WriteLine($"App will exit at {(DateTime.Now + until).TimeOfDay}");
        await Task.Delay(until);
    }
    else
    {
        Console.WriteLine("Press any key to exit...");
        Console.ReadLine();
    }
}