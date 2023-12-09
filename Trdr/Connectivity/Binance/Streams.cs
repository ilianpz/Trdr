using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Trdr.Connectivity.Binance;

public static class Streams
{
    public static async IAsyncEnumerable<Ticker> SubscribeTicker(
        string symbol,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (symbol == null) throw new ArgumentNullException(nameof(symbol));

        var connection = Connection.Create($"{symbol.ToLower(CultureInfo.InvariantCulture)}@bookTicker");
        await connection.Connect(cancellationToken).ConfigureAwait(false);

        await foreach (string message in connection.ReceiveMessages(cancellationToken))
        {
            yield return JsonSerializer.Deserialize<Ticker>(message)!;
        }
    }
}