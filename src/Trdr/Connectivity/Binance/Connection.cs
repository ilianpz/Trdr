using System.Reactive.Linq;
using Nito.AsyncEx;
using Nito.Disposables;

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

    public Task<string> GetNextMessage(CancellationToken cancellationToken = default)
    {
        return _receiver.DequeueAsync(cancellationToken);
    }

    public IObservable<string> GetMessages()
    {
        return Observable.Create<string>(
            async observer =>
            {
                var cts = new CancellationTokenSource();

                try
                {
                    string message = await _receiver.DequeueAsync(cts.Token).ConfigureAwait(false);
                    observer.OnNext(message);
                }
                catch (Exception ex)
                {
                    if (cts.IsCancellationRequested)
                        observer.OnCompleted();
                    else
                        observer.OnError(ex);
                }

                return Disposable.Create(
                    () =>
                    {
                        cts.Cancel();
                        Disconnect(CancellationToken.None);
                    });
            });
    }

    protected override Task OnMessageReceived(string msgStr)
    {
        _receiver.EnqueueAsync(msgStr);
        return Task.CompletedTask;
    }
}