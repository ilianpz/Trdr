# Trdr
An work-in-progress trading bot framework for .NET.

Trdr aims to:
1. Make it easier to handle asynchronous events when implementing trading strategies.
2. Allow strategies to be back-testable and unit-testable.

## Sentinel

```csharp
     protected override Task Run(CancellationToken cancellationToken)
    {
        // Subscribe to Binance's and CoinJar's tickers.
        return SubscribeLatest(
            _binanceTicker.ZipWithLatest(_coinJarTicker),
            items =>
            {
                var binanceTicker = items.Item1.Value;
                var coinJarTicker = items.Item2.Value;
                decimal buy = binanceTicker.Ask;
                decimal sell = coinJarTicker.Bid;
                return HandleUpdate(buy, sell);
            },
            cancellationToken);
    }

    private async Task HandleUpdate(decimal buy, decimal sell)
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
```
## Testability
```csharp
    public async Task Can_buy_and_sell()
    {
        // Mock a source of ticker events.
        var binanceSubject = new Subject<Timestamped<Ticker>>();
        var coinJarSubject = new Subject<Timestamped<TickerPayload>>();

        // Count events so we can test the scenario reliably.
        var countdown = new AsyncCountdownEvent(4);

        int buyAtBinance = 0;
        int sellAtCoinJar = 0;

        var strategy = new SimpleArbitrageStrategy(
            coinJarSubject,
            binanceSubject,
            _ =>
            {
                ++buyAtBinance;
                countdown.Signal();
                return Task.CompletedTask;
            },
            _ =>
            {
                ++sellAtCoinJar;
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
        Assert.That(buyAtBinance, Is.EqualTo(2));
        Assert.That(sellAtCoinJar, Is.EqualTo(2));
    }
```

## Connectivity

Trdr currently has bare-bones connectivity to the following exchanges:

1. Binance (WebSocket streams)
2. CoinJar (WebSocket API)