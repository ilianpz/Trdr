﻿using Trdr.Connectivity.Binance;
using Trdr.Connectivity.CoinJar.Phoenix;
using Trdr.Reactive;

namespace Trdr.Samples.SimpleArbitrage;

public sealed class SimpleArbitrageStrategy : Strategy
{
    private readonly IObservable<TickerPayload> _coinJarTicker;
    private readonly IObservable<Ticker> _binanceTicker;
    private readonly Action<decimal> _buyAtBinance;
    private readonly Action<decimal> _sellAtCoinJar;

    public SimpleArbitrageStrategy(
        IObservable<TickerPayload> coinJarTicker,
        IObservable<Ticker> binanceTicker,
        Action<decimal> buyAtBinance,
        Action<decimal> sellAtCoinJar)
    {
        _coinJarTicker = coinJarTicker ?? throw new ArgumentNullException(nameof(coinJarTicker));
        _binanceTicker = binanceTicker ?? throw new ArgumentNullException(nameof(binanceTicker));
        _buyAtBinance = buyAtBinance ?? throw new ArgumentNullException(nameof(buyAtBinance));
        _sellAtCoinJar = sellAtCoinJar ?? throw new ArgumentNullException(nameof(sellAtCoinJar));
    }

    protected override Task Run(CancellationToken cancellationToken)
    {
        // Subscribe to Binance's and CoinJar's tickers.
        return Subscribe(
            _binanceTicker.ZipWithLatest(_coinJarTicker),
            items =>
            {
                decimal buy = items.Item1.Ask;
                decimal sell = items.Item2.Bid;
                HandleUpdate(buy, sell);
            },
            cancellationToken);
    }

    private void HandleUpdate(decimal buy, decimal sell)
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
            _buyAtBinance(buy);
            _sellAtCoinJar(sell);
        }
    }
}