using Microsoft.Extensions.Logging;
using Trdr.App;
using Trdr.Async;
using Trdr.Connectivity.CoinJar;

var loggerFactory = Application.SetupDefaultLogger();
var logger = loggerFactory.CreateLogger("Main");

logger.LogInformation("Started...");
var connection = Connection.Create();
await connection.Connect();

const string symbol = "XRPUSDT";
SubscribeTicker().Forget();
SubscribeTrades().Forget();

Console.ReadLine();

async Task SubscribeTicker()
{
    logger.LogInformation("SubscribeTicker");

    await foreach (var messagePair in connection.SubscribeTickerRaw(symbol))
    {
        Console.WriteLine(messagePair.RawMessage);
        logger.LogInformation("{RawMessage}", messagePair.RawMessage);
    }
}

async Task SubscribeTrades()
{
    logger.LogInformation("SubscribeTrades");

    await foreach (var messagePair in connection.SubscribeTradesRaw(symbol))
    {
        Console.WriteLine(messagePair.RawMessage);
        logger.LogInformation("{RawMessage}", messagePair.RawMessage);
    }
}