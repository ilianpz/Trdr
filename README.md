# Trdr
An experimental work-in-progress trading bot framework for .Net.

Trdr aims to:
1. Make it easier to handle asynchronous events when implementing trading strategies.
2. Allow strategies to be unit-testable.

## Sentinel

It uses the concept of a `Sentinel` which watches conditions asynchronously. See the following sample code:

```csharp
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

            // Buy at Binance then sell at CoinJar
        } while (await sentinel.NextEvent(cancellationToken));
    }
```

## Connectivity

Trdr currently has bare-bones connectivity to the following exchanges:

1. Binance (WebSocket streams)
2. CoinJar (WebSocket API)
