using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Trdr.Connectivity.Binance;

/// <summary>
/// Binance WebSocket streams API.
///
/// See https://github.com/binance/binance-spot-api-docs/blob/master/web-socket-streams.md.
/// </summary>
public static class Streams
{
    public static async IAsyncEnumerable<Ticker> SubscribeTicker(
        string symbol,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (symbol == null) throw new ArgumentNullException(nameof(symbol));
        await foreach (string message in SubscribeRaw(GetStreamName(symbol, "bookTicker"), cancellationToken))
        {
            yield return JsonSerializer.Deserialize<Ticker>(message)!;
        }
    }

    public static async IAsyncEnumerable<string> SubscribeRaw(
        string streamName,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (streamName == null) throw new ArgumentNullException(nameof(streamName));

        var connection = Connection.Create(streamName);
        await connection.Connect(cancellationToken).ConfigureAwait(false);

        await foreach (string message in connection.ReceiveMessages(cancellationToken))
        {
            yield return message;
        }
    }

    private static string GetStreamName(string symbol, string stream)
    {
        return $"{symbol.ToLower(CultureInfo.InvariantCulture)}@{stream}";
    }
}