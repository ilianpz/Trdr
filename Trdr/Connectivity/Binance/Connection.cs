using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace Trdr.Connectivity.Binance;

public sealed class Connection : WebSocketConnection
{
    private readonly AsyncProducerConsumerQueue<string> _receiver = new();

    private Connection(string connectionStr)
        : base(connectionStr, Module.CreateLogger<Connection>()!)
    {
    }

    public static Connection Create(string streamName)
    {
        if (streamName == null) throw new ArgumentNullException(nameof(streamName));
        return new Connection($"wss://stream.binance.com:443/ws/{streamName}");
    }

    public async IAsyncEnumerable<string> ReceiveMessages(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (true)
        {
            string message = await _receiver.DequeueAsync(cancellationToken).ConfigureAwait(false);
            yield return message;
        }
    }

    protected override Task OnMessageReceived(string msgStr)
    {
        _receiver.EnqueueAsync(msgStr);
        return Task.CompletedTask;
    }
}