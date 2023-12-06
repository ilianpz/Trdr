# Trdr
An experimental trading bot framework for .Net.

Trdr aims to:
1. Make it easier to handle asynchronous events when implementing trading strategies.
2. Allow strategies to be unit-testable.

It uses the concept of a `Sentinel` which watches conditions asynchronously. See the following sample code:

```csharp
    var sentinel = new Sentinel();

    // Subscribe to Binance's ticker and store its ask everytime it's updated.
    decimal buy = 0;
    sentinel.Subscribe(_binanceTicker, ticker => buy = ticker.Ask);

    // Subscribe to CoinJar's ticker and store its bid everytime it's updated.
    decimal sell = 0;
    sentinel.Subscribe(_coinJarTicker, ticker => sell = ticker.Bid);

    sentinel.Start(); // Start the subscriptions

    // This simple strategy waits for an arbitrage opportunity by buying low at Binance
    // and selling high at CoinJar.
    //
    // Note: this is a toy trading strategy that will not work in the real world.
    // It also ignores transaction fees and bid/ask quantities.
    // This only serves to illustrate how the framework is meant to be used. 

    while (true)
    {
        await sentinel.Watch(
            () =>
                sell > 0 && buy > 0 && // <-- Wait for both events
                sell - buy > 0.002m,
            cancellationToken);

        // Buy at Binance then sell at CoinJar
    }
```
