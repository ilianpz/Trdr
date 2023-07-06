using Microsoft.Extensions.Logging;
using Trdr;
using Trdr.App;
using Trdr.Connectivity.CoinJar;


var loggerFactory = Application.SetupDefaultLogger();
var logger = loggerFactory.CreateLogger("Main");

logger.LogInformation("Started...");
var connection = Connection.Create();
await connection.Connect();

const string pair = "XRPUSDT";
SubscribeTicker().Forget();
SubscribeTrades().Forget();

Console.ReadLine();

async Task SubscribeTicker()
{
    logger.LogInformation("SubscribeTicker");

    await foreach (var msg in connection.SubscribeTicker(pair))
    {
        logger.LogInformation("{msg}", msg);
    }
}

async Task SubscribeTrades()
{
    logger.LogInformation("SubscribeTrades");

    await foreach (var msg in connection.SubscribeTrades(pair))
    {
        logger.LogInformation("{msg}", msg);
    }
}
