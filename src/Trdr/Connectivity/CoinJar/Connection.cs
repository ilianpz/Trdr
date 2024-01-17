using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Trdr.Async;
using Trdr.Connectivity.CoinJar.Phoenix;
using Trdr.Reactive;

namespace Trdr.Connectivity.CoinJar;

/// <summary>
/// Represents a WebSocket connection to CoinJar
/// </summary>
/// <remarks>
/// The CoinJar WebSocket API uses Phoenix channels (https://hexdocs.pm/phoenix/channels.html).
/// </remarks>
public sealed class Connection : WebSocketConnection
{
    private static readonly JsonSerializerOptions _serializerOptions = new();
    
    private readonly Dictionary<string, AsyncProducerConsumerQueue<MessagePair>> _topicToReceiver = new();

    private long _ref;

    static Connection()
    {
        _serializerOptions.Converters.Add(new MessageConverter());
    }

    private Connection(string connectionStr)
        : base(connectionStr, Module.CreateLogger<Connection>()!)
    {
    }

    public static Connection Create()
    {
        return new Connection("wss://feed.exchange.coinjar.com/socket/websocket");
    }

    public static Connection CreateSandbox()
    {
        return new Connection("wss://feed.exchange.coinjar-sandbox.com/socket/websocket");
    }

    public IObservable<Timestamped<MessagePair>> GetChannel(string channel)
    {
        if (channel == null) throw new ArgumentNullException(nameof(channel));

        return Observable.Create<Timestamped<MessagePair>>(
            async observer =>
            {
                var cts = new CancellationTokenSource();

                try
                {
                    var request = CreateJoinRequest(channel);

                    var receiver = new AsyncProducerConsumerQueue<MessagePair>();
                    lock (_topicToReceiver)
                    {
                        _topicToReceiver[channel] = receiver;
                    }

                    await WebSocket.SendString(request, cts.Token).ConfigureAwait(false);

                    while (true)
                    {
                        var messagePair = await receiver.DequeueAsync(cts.Token).ConfigureAwait(false);
                        if (messagePair.Message is MessageWithPayload<ReplyPayload> replyMessage)
                        {
                            if (replyMessage.Payload.Status != "ok")
                                throw new InvalidOperationException("Received error response");
                        }

                        observer.OnNext(messagePair.Timestamp());
                    }
                }
                catch (Exception ex)
                {
                    if (cts.IsCancellationRequested)
                        observer.OnCompleted();
                    else
                        observer.OnError(ex);
                }

                return Disposable.Create(cts, theCts => theCts.Cancel());
            });
    }

    public IObservable<Timestamped<TickerPayload>> SubscribeTicker(string symbol)
    {
        return SubscribeTickerRaw(symbol)
            .Select(
                messagePair =>
                    Timestamped.Create(
                        ((MessageWithPayload<TickerPayload>)messagePair.Value.Message).Payload,
                        messagePair.Timestamp));
    }

    public IObservable<Timestamped<MessagePair>> SubscribeTickerRaw(string pair)
    {
        if (pair == null) throw new ArgumentNullException(nameof(pair));

        string topic = $"{TopicType.Ticker}:{pair}";
        return GetChannel(topic);
    }

    public IObservable<Timestamped<MessagePair>> SubscribeTradesRaw(string pair)
    {
        if (pair == null) throw new ArgumentNullException(nameof(pair));

        string topic = $"{TopicType.Trades}:{pair}";
        return GetChannel(topic);
    }

    protected override void OnConnected()
    {
        DoHeartbeat().Forget();
    }

    protected override async Task OnMessageReceived(string msgStr)
    {
        try
        {
            var message = JsonSerializer.Deserialize<Message>(msgStr, _serializerOptions)!;
            AsyncProducerConsumerQueue<MessagePair>? messages;
            lock (_topicToReceiver)
            {
                _topicToReceiver.TryGetValue(message.Topic, out messages);
            }

            if (messages != null)
                await messages.EnqueueAsync(new MessagePair { Message = message, RawMessage = msgStr })
                    .ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Unable to parse message");
        }
    }

    private static string CreateRequest(string topic, string @event, long @ref)
    {
        return $"{{ \"topic\": \"{topic}\", \"event\": \"{@event}\", \"payload\": {{}}, \"ref\": {@ref} }}";
    }

    private string CreateJoinRequest(string topic)
    {
        return CreateRequest(topic, EventType.Join, GetRef());
    }

    private string CreateHeartbeatMsg()
    {
        return CreateRequest("phoenix", EventType.HeartBeat, GetRef());
    }

    private long GetRef() => Interlocked.Increment(ref _ref);

    private async Task DoHeartbeat()
    {
        try
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(45)).ConfigureAwait(false);
                
                Logger.LogDebug("Sending heartbeat");
                await WebSocket.SendString(CreateHeartbeatMsg());
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Heartbeat failed");
        }
    }
}