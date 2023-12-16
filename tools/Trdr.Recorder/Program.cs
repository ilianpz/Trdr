using System.CommandLine;
using Trdr.App;
using Trdr.Recorder;

var loggerFactory = Application.SetupDefaultLogger();
var binanceHandler = new BinanceHandler(loggerFactory.CreateLogger(nameof(BinanceHandler)));
var coinJarHandler = new CoinJarHandler(loggerFactory.CreateLogger(nameof(CoinJarHandler)));
TimeSpan until = TimeSpan.Zero;

int? result = ParseCommandLine();
if (result != 0)
    return result.Value;

try
{
    result = binanceHandler.Execute();
    if (result.HasValue)
        return result.Value;

    result = await coinJarHandler.Execute();
    if (result.HasValue)
        return result.Value;
}
finally
{
    if (result != null)
    {
        await End();
    }
}

return 0;   // Unreachable

int ParseCommandLine()
{
    var rootCmd = new RootCommand { binanceHandler.GetCommand(), coinJarHandler.GetCommand() };
    var untilOption = new Option<TimeSpan>("--until");
    rootCmd.AddGlobalOption(untilOption);

    return rootCmd.Invoke(args);
}

Task End()
{
    Console.WriteLine("Running...");

    if (until > TimeSpan.Zero)
    {
        Console.WriteLine($"App will exit at {(DateTime.Now + until).TimeOfDay}");
        return Task.Delay(until);
    }

    Console.WriteLine("Press any key to exit...");
    Console.ReadLine();

    return Task.CompletedTask;
}