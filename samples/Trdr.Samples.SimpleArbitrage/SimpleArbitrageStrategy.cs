using System.Reactive;
using Trdr.Connectivity.Binance;
using Trdr.Connectivity.CoinJar.Phoenix;
using Trdr.Reactive;

namespace Trdr.Samples.SimpleArbitrage;

public sealed class SimpleArbitrageStrategy : Strategy
{
    private readonly IObservable<Timestamped<TickerPayload>> _coinJarTicker;
    private readonly IObservable<Timestamped<Ticker>> _binanceTicker;
    private readonly Func<decimal, Task> _buyAtBinance;
    private readonly Func<decimal, Task> _sellAtCoinJar;

    public SimpleArbitrageStrategy(
        IObservable<Timestamped<TickerPayload>> coinJarTicker,
        IObservable<Timestamped<Ticker>> binanceTicker,
        Func<decimal, Task> buyAtBinance,
        Func<decimal, Task> sellAtCoinJar)
    {
        _coinJarTicker = coinJarTicker ?? throw new ArgumentNullException(nameof(coinJarTicker));
        _binanceTicker = binanceTicker ?? throw new ArgumentNullException(nameof(binanceTicker));
        _buyAtBinance = buyAtBinance ?? throw new ArgumentNullException(nameof(buyAtBinance));
        _sellAtCoinJar = sellAtCoinJar ?? throw new ArgumentNullException(nameof(sellAtCoinJar));
    }

    protected override Task Run(CancellationToken cancellationToken)
    {
        // Subscribe to Binance's and CoinJar's tickers. For each
        // latest pair of updates from both exchanges, check if we have
        // an opportunity to get a profit.
        return SubscribeLatest(
            _binanceTicker.ZipWithLatest(_coinJarTicker),
            items =>
            {
                var binanceTicker = items.Item1.Value;
                var coinJarTicker = items.Item2.Value;
                decimal buy = binanceTicker.Ask;
                decimal sell = coinJarTicker.Bid;
                return TryBuyAndSell(buy, sell);
            },
            cancellationToken);
    }

    private async Task TryBuyAndSell(decimal buy, decimal sell)
    {
        // This simple strategy waits for an arbitrage opportunity by buying low at Binance
        // and selling high at CoinJar.
        //
        // Note: this is a toy trading strategy that will not work in the real world.
        // It also ignores transaction fees and bid/ask quantities.
        // This only serves to illustrate how the framework is meant to be used.
        if (sell - buy > 0.002m)
        {
            // Buy at Binance then sell at CoinJar
            await Task.WhenAll(_buyAtBinance(buy), _sellAtCoinJar(sell));
        }
    }
}