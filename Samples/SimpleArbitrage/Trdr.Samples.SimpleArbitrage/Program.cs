using Trdr.Samples.SimpleArbitrage;
using Binance = Trdr.Connectivity.Binance.Connection;
using CoinJar = Trdr.Connectivity.CoinJar.Connection;

var binance = Binance.Create();
await binance.Connect();

var coinJar = CoinJar.Create();
await coinJar.Connect();

const string symbol = "XRPUSDT";

var coinJarTicker = coinJar.SubscribeTicker(symbol);
var binanceTicker = binance.SubscribeTicker(symbol);

var strategy = new SimpleArbitrageStrategy(
    coinJarTicker,
    binanceTicker,
    buy => Console.WriteLine($"Buy at Binance @ {buy}"),
    sell => Console.WriteLine($"Sell at CoinJar @ {sell}"));
await strategy.Run();