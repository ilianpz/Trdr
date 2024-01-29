using System.Reactive;
using System.Reactive.Subjects;
using Nito.AsyncEx;
using Trdr.Connectivity.Binance;
using Trdr.Connectivity.CoinJar.Phoenix;
using Trdr.Reactive;
using Trdr.Samples.SimpleArbitrage;

namespace Trdr.Samples.Tests;

[TestFixture]
public class SimpleArbitrageStrategyTests
{
    [Test]
    [Repeat(100)] // Try to catch race conditions
    public async Task Can_buy_and_sell()
    {
        // Mock a source of ticker events.
        var binanceSubject = new Subject<Timestamped<Ticker>>();
        var coinJarSubject = new Subject<Timestamped<TickerPayload>>();

        // Count events so we can test the scenario reliably.
        var countdown = new AsyncCountdownEvent(4);

        var buys = new List<decimal>();
        var sells = new List<decimal>();

        var strategy = new SimpleArbitrageStrategy(
            coinJarSubject,
            binanceSubject,
            buyAtBinance =>
            {
                buys.Add(buyAtBinance);
                countdown.Signal();
                return Task.CompletedTask;
            },
            sellAtCoinJar =>
            {
                sells.Add(sellAtCoinJar);
                countdown.Signal();
                return Task.CompletedTask;
            });

        await strategy.Start();

        // Generate events that will trigger a buy and sell.
        binanceSubject.OnNext(new Ticker { AskRaw = "9.200", BidRaw = "9.005" }.Timestamp());
        coinJarSubject.OnNext(new TickerPayload { AskRaw = "9.505", BidRaw = "9.205" }.Timestamp());

        // Generate events that will NOT trigger a buy and sell.
        binanceSubject.OnNext(new Ticker { AskRaw = "9.200", BidRaw = "9.005" }.Timestamp());
        coinJarSubject.OnNext(new TickerPayload { AskRaw = "9.505", BidRaw = "9.201" }.Timestamp());

        // Generate events that will trigger a buy and sell.
        binanceSubject.OnNext(new Ticker { AskRaw = "9.200", BidRaw = "9.005" }.Timestamp());
        coinJarSubject.OnNext(new TickerPayload { AskRaw = "9.505", BidRaw = "9.205" }.Timestamp());

        await countdown.WaitAsync();

        // Strategy should have bought and sold twice.
        Assert.That(buys, Has.Count.EqualTo(2));
        Assert.That(buys[0], Is.EqualTo(9.2m));
        Assert.That(buys[1], Is.EqualTo(9.2m));
        Assert.That(sells, Has.Count.EqualTo(2));
        Assert.That(sells[0], Is.EqualTo(9.205m));
        Assert.That(sells[1], Is.EqualTo(9.205m));
    }
}