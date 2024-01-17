using System.Globalization;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.Json;
using Trdr.Reactive;

namespace Trdr.Connectivity.Binance;

/// <summary>
/// Binance WebSocket streams API.
///
/// See https://github.com/binance/binance-spot-api-docs/blob/master/web-socket-streams.md.
/// </summary>
public static class Streams
{
    public static IObservable<Timestamped<Ticker>> GetTicker(string symbol)
    {
        if (symbol == null) throw new ArgumentNullException(nameof(symbol));

        return GetStream(GetStreamName(symbol, "bookTicker"))
            .Select(timeStamped =>
                Timestamped.Create(
                    JsonSerializer.Deserialize<Ticker>(timeStamped.Value)!,
                    timeStamped.Timestamp));
    }

    public static IObservable<Timestamped<string>> GetStream(string streamName)
    {
        if (streamName == null) throw new ArgumentNullException(nameof(streamName));

        return Observable.Create<Timestamped<string>>(
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
                        observer.OnNext(message.Timestamp());
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