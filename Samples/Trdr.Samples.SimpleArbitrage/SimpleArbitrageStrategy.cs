using Trdr.Connectivity.Binance;
using Trdr.Connectivity.CoinJar.Phoenix;

namespace Trdr.Samples.SimpleArbitrage;

public sealed class SimpleArbitrageStrategy
{
    private readonly IAsyncEnumerable<TickerPayload> _coinJarTicker;
    private readonly IAsyncEnumerable<Ticker> _binanceTicker;
    private readonly Action<decimal> _buyAtBinance;
    private readonly Action<decimal> _sellAtCoinJar;

    public SimpleArbitrageStrategy(
        IAsyncEnumerable<TickerPayload> coinJarTicker,
        IAsyncEnumerable<Ticker> binanceTicker,
        Action<decimal> buyAtBinance,
        Action<decimal> sellAtCoinJar)
    {
        _coinJarTicker = coinJarTicker ?? throw new ArgumentNullException(nameof(coinJarTicker));
        _binanceTicker = binanceTicker ?? throw new ArgumentNullException(nameof(binanceTicker));
        _buyAtBinance = buyAtBinance ?? throw new ArgumentNullException(nameof(buyAtBinance));
        _sellAtCoinJar = sellAtCoinJar ?? throw new ArgumentNullException(nameof(sellAtCoinJar));
    }

    public async Task Run(CancellationToken cancellationToken = default)
    {
        var sentinel = new Sentinel();

        // Subscribe to Binance's ticker and store its ask everytime it's updated.
        decimal buy = 0;
        sentinel.Subscribe(_binanceTicker, ticker => buy = ticker.Ask);

        // Subscribe to CoinJar's ticker and store its bid everytime it's updated.
        decimal sell = 0;
        sentinel.Subscribe(_coinJarTicker, ticker => sell = ticker.Bid);

        await sentinel.Start(); // Start the subscriptions

        // This simple strategy waits for an arbitrage opportunity by buying low at Binance
        // and selling high at CoinJar.
        //
        // Note: this is a toy trading strategy that will not work in the real world.
        // It also ignores transaction fees and bid/ask quantities.
        // This only serves to illustrate how the framework is meant to be used.

        do
        {
            await sentinel.Watch(() => sell - buy > 0.002m, cancellationToken);

            _buyAtBinance(buy);
            _sellAtCoinJar(sell);
        } while (await sentinel.NextEvent(cancellationToken));
    }
}