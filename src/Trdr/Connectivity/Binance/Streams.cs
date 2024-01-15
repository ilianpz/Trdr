using System.Globalization;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.Json;

namespace Trdr.Connectivity.Binance;

/// <summary>
/// Binance WebSocket streams API.
///
/// See https://github.com/binance/binance-spot-api-docs/blob/master/web-socket-streams.md.
/// </summary>
public static class Streams
{
    public static IObservable<Ticker> GetTicker(string symbol)
    {
        if (symbol == null) throw new ArgumentNullException(nameof(symbol));

        return GetStream(GetStreamName(symbol, "bookTicker"))
            .Select(message => JsonSerializer.Deserialize<Ticker>(message)!);
    }

    public static IObservable<string> GetStream(string streamName)
    {
        if (streamName == null) throw new ArgumentNullException(nameof(streamName));

        return Observable.Create<string>(
            async observer =>
            {
                var cts = new CancellationTokenSource();
                Connection? connection = null;

                try
                {
                    connection = Connection.Create(streamName);
                    await connection.Connect(cts.Token).ConfigureAwait(false);

                    while (true)
                    {
                        var message = await connection.GetNextMessage(cts.Token).ConfigureAwait(false);
                        observer.OnNext(message);
                    }
                }
                catch (Exception ex)
                {
                    if (cts.IsCancellationRequested)
                        observer.OnCompleted();
                    else
                        observer.OnError(ex);
                }

                return Disposable.Create(
                    (cts, connection),
                    state =>
                    {
                        state.cts.Cancel();
                        state.connection?.Disconnect(CancellationToken.None);
                    });
            });
    }

    private static string GetStreamName(string symbol, string stream)
    {
        return $"{symbol.ToLower(CultureInfo.InvariantCulture)}@{stream}";
    }
}