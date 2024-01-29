# Trdr
A work-in-progress trading bot framework for .NET.

## Goals
The core of Trdr is based on [ReactiveX](https://reactivex.io/). The goal is twofold:

1. Make it easier to handle asynchronous events when implementing strategies.
2. Make it easier to back test and unit test strategies.

## Implementing a bot
```csharp
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
        // This strategy waits for a simple arbitrage opportunity by buying low at Binance
        // and selling high at CoinJar.
        //
        // Note: this is a toy trading strategy that will not work in the real world.
        // It also ignores transaction fees and bid/ask quantities.
        // This only serves to illustrate how the framework is meant to be used.
        if (sell - buy > 0.002m)
        {
            // Buy at Binance then sell at CoinJar and wait for the orders
            // be filled.
            await Task.WhenAll(_buyAtBinance(buy), _sellAtCoinJar(sell));
        }
    }
```
## Testing
```csharp
    [Test]
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
```
## Connectivity
Trdr currently has bare-bones connectivity to the following exchanges:

1. Binance (WebSocket streams)
2. CoinJar (WebSocket API)

## Tools
Trdr comes with a couple of tools to record real-time data from exchanges and manipulate them.

### Record
Record logs data streams from exchanges. The following captures a WebSocket trade stream from Binance:
```
.\record.exe binance xxxxxx@trade --log .\bin_xxxxx.log

2024-01-19T11:08:33.2019691 [INF] [4] [BinanceHandler] {"e":"trade","E":1705662513193,"s":"XXXXXX","t":3380132513,"p":"41474.37000000","q":"0.00482000","b":24397178050,"a":24397177595,"T":1705662513193,"m":false,"M":true}
2024-01-19T11:08:33.3578513 [INF] [6] [BinanceHandler] {"e":"trade","E":1705662513361,"s":"XXXXXX","t":3380132514,"p":"41474.36000000","q":"0.00049000","b":24397177737,"a":24397178058,"T":1705662513360,"m":true,"M":true}
2024-01-19T11:08:33.3595085 [INF] [6] [BinanceHandler] {"e":"trade","E":1705662513361,"s":"XXXXXX","t":3380132515,"p":"41474.36000000","q":"0.00470000","b":24397177741,"a":24397178058,"T":1705662513360,"m":true,"M":true}
2024-01-19T11:08:33.5025322 [INF] [6] [BinanceHandler] {"e":"trade","E":1705662513505,"s":"XXXXXX","t":3380132516,"p":"41474.36000000","q":"0.00048000","b":24397177743,"a":24397178068,"T":1705662513503,"m":true,"M":true}
```

### Strip
Strip parses the JSON component of a Record log file and outputs them into CSV format. The following strips the
price ("p") and quantity ("q") fields from Binance's JSON payload:

```
.\strip.exe .\bin_xxxxxx.log --tokens p q > .\bin_xxxxxx.csv

2024-01-19T11:08:33.2019691,41474.37000000,0.00482000
2024-01-19T11:08:33.3578513,41474.36000000,0.00049000
2024-01-19T11:08:33.3595085,41474.36000000,0.00470000
2024-01-19T11:08:33.5025322,41474.36000000,0.00048000
```

### Aggr
Aggr aggregates CSV rows according to a given function performed on the first given column. The following
groups rows with the same timestamp when rounded up to 10ms windows. The price column (1) is then averaged
and the quantity column (2) is summed.

```
.\aggr.exe .\bin_xxxxxx.csv -c "0,ts,rndup{00:00:00.01}" -c "1,d,mean" -c "2,d,sum" > .\bin_xxxxxx.aggr.csv

2024-01-19T11:08:33.2100000,41474.37000000,0.00482000
2024-01-19T11:08:33.3600000,41474.36000000,0.00519000
2024-01-19T11:08:33.5100000,41474.36000000,0.00048000
```
## TODOs

1. Incorporate Python for data analysis
2. Implement order API to exchanges