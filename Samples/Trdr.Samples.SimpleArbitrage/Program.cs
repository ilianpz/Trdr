using Trdr.Samples.SimpleArbitrage;
using Binance = Trdr.Connectivity.Binance.Streams;
using CoinJar = Trdr.Connectivity.CoinJar.Connection;

var coinJar = CoinJar.Create();
await coinJar.Connect();

const string symbol = "XRPUSDT";

var coinJarTicker = coinJar.SubscribeTicker(symbol);
var binanceTicker = Binance.SubscribeTicker(symbol);

var strategy = new SimpleArbitrageStrategy(
    coinJarTicker,
    binanceTicker,
    buy => Console.WriteLine($"Buy at Binance @ {buy}"),
    sell => Console.WriteLine($"Sell at CoinJar @ {sell}"));
await strategy.Run();