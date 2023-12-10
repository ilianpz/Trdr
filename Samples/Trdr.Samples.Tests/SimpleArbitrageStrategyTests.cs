using System.Reactive.Subjects;
using Nito.AsyncEx;
using Trdr.Connectivity.Binance;
using Trdr.Connectivity.CoinJar.Phoenix;
using Trdr.Samples.SimpleArbitrage;

namespace Trdr.Samples.Tests;

[TestFixture]
public class SimpleArbitrageStrategyTests
{
    [Test]
    public async Task Can_buy_and_sell()
    {
        var binanceSubject = new Subject<Ticker>();
        var coinJarSubject = new Subject<TickerPayload>();

        var countdown = new AsyncCountdownEvent(2);

        int buyAtBinance = 0;
        int sellAtCoinJar = 0;

        var strategy = new SimpleArbitrageStrategy(
            coinJarSubject.ToAsyncEnumerable(),
            binanceSubject.ToAsyncEnumerable(),
            _ =>
            {
                ++buyAtBinance;
                countdown.Signal();
            },
            _ =>
            {
                ++sellAtCoinJar;
                countdown.Signal();
            });

        strategy.Run().Forget();

        binanceSubject.OnNext(new Ticker { AskRaw = "10", BidRaw = "9.5m" });
        coinJarSubject.OnNext(new TickerPayload { AskRaw = "12", BidRaw = "11" });

        await countdown.WaitAsync();
        Assert.That(buyAtBinance, Is.EqualTo(1));
        Assert.That(sellAtCoinJar, Is.EqualTo(1));
    }
}