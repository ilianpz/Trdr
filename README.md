# Trdr
An work-in-progress trading bot framework for .NET.

Trdr aims to:
1. Make it easier to handle asynchronous events when implementing trading strategies.
2. Allow strategies to be back-testable and unit-testable.

## Sentinel

It uses the concept of a `Sentinel` which watches conditions asynchronously. See the following sample code:

```csharp
    protected override async Task Run(CancellationToken cancellationToken)
    {
        decimal buy = 0;
        decimal sell = 0;

        // Subscribe to Binance's ticker and store its ask everytime it's updated.
        // Subscribe to CoinJar's ticker and store its bid everytime it's updated.
        var sentinel =
            CreateSentinel(_binanceTicker, ticker => buy = ticker.Ask)
                .Combine(_coinJarTicker, ticker => sell = ticker.Bid);

        sentinel.Start(); // Start the subscriptions

        // This simple strategy waits for an arbitrage opportunity by buying low at Binance
        // and selling high at CoinJar.
        //
        // Note: this is a toy trading strategy that will not work in the real world.
        // It also ignores transaction fees and bid/ask quantities.
        // This only serves to illustrate how the framework is meant to be used.

        do
        {
            await sentinel.Watch(() => sell - buy > 0.002m, cancellationToken);

            // Buy at Binance then sell at CoinJar
            _buyAtBinance(buy);
            _sellAtCoinJar(sell);
        } while (true);

        // Exits only when cancellationToken is cancelled.
    }
```

## Testability



```csharp
    public async Task Can_buy_and_sell()
    {
        // Mock a source of ticker events.
        var binanceSubject = new Subject<Ticker>();
        var coinJarSubject = new Subject<TickerPayload>();

        // Synchronize so we can test the scenario reliably.
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

        await strategy.Start();

        // Generate events that will trigger a buy and sell.
        binanceSubject.OnNext(new Ticker { AskRaw = "9.200", BidRaw = "9.005" });
        coinJarSubject.OnNext(new TickerPayload { AskRaw = "9.505", BidRaw = "9.205" });

        // Wait for the strategy to process the events
        await countdown.WaitAsync();
        countdown.AddCount(2);

        // Generate events that will NOT trigger a buy and sell.
        binanceSubject.OnNext(new Ticker { AskRaw = "9.200", BidRaw = "9.005" });
        coinJarSubject.OnNext(new TickerPayload { AskRaw = "9.505", BidRaw = "9.201" });

        // Generate events that will trigger a buy and sell.
        binanceSubject.OnNext(new Ticker { AskRaw = "9.200", BidRaw = "9.005" });
        coinJarSubject.OnNext(new TickerPayload { AskRaw = "9.505", BidRaw = "9.205" });

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