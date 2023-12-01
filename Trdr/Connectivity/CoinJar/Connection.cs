using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Trdr.Connectivity.CoinJar.Phoenix;

namespace Trdr.Connectivity.CoinJar;

/// <summary>
/// Represents a WebSocket connection to CoinJar
/// </summary>
/// <remarks>
/// The CoinJar WebSocket API uses Phoenix channels (https://hexdocs.pm/phoenix/channels.html).
/// </remarks>
public sealed class Connection
{
    private static readonly JsonSerializerOptions _serializerOptions = new();
    
    private readonly ILogger<Connection>? _logger;

    private readonly ClientWebSocket _webSocket = new();
    private readonly string _connectionStr;
    private readonly ArraySegment<byte> _buffer = new(new byte[2048]);
    private readonly Dictionary<string, AsyncProducerConsumerQueue<MessagePair>> _topicToReceiver = new();

    private long _ref;

    static Connection()
    {
        _serializerOptions.Converters.Add(new MessageConverter());
    }

    private Connection(string connectionStr)
    {
        _logger = Module.CreateLogger<Connection>();
        _connectionStr = connectionStr;
    }

    public static Connection Create()
    {
        return new Connection("wss://feed.exchange.coinjar.com/socket/websocket");
    }

    public static Connection CreateSandbox()
    {
        return new Connection("wss://feed.exchange.coinjar-sandbox.com/socket/websocket");
    }

    public async Task Connect()
    {
        _logger?.LogInformation("Connecting...");
      
        await _webSocket.ConnectAsync(new Uri(_connectionStr), CancellationToken.None).ConfigureAwait(false);

        Listen().Forget();
        DoHeartbeat().Forget();

        _logger?.LogInformation("Connected");
    }

    public Task Disconnect()
    {
        return _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal disconnect", CancellationToken.None);
    }

    public IAsyncEnumerable<MessagePair> Subscribe(string channel)
    {
        return Subscribe(channel, CancellationToken.None);
    }

    public async IAsyncEnumerable<MessagePair> Subscribe(
        string channel,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var request = CreateJoinRequest(channel);

        var receiver = new AsyncProducerConsumerQueue<MessagePair>();
        lock (_topicToReceiver)
        {
            _topicToReceiver[channel] = receiver;
        }

        await _webSocket.SendString(request).ConfigureAwait(false);

        while (true)
        {
            var messagePair = await receiver.DequeueAsync(cancellationToken);
            if (messagePair.Message is MessageWithPayload<ReplyPayload> replyMessage)
            {
                if (replyMessage.Payload.Status != "ok")
                    throw new InvalidOperationException("Received error response");
            }

            yield return messagePair;
        }
    }

    public IAsyncEnumerable<TickerPayload> SubscribeTicker(string symbol)
    {
        return SubscribeTickerRaw(symbol)
            .Select(messagePair => ((MessageWithPayload<TickerPayload>)messagePair.Message).Payload);
    }

    public IAsyncEnumerable<MessagePair> SubscribeTickerRaw(string pair)
    {
        if (pair == null) throw new ArgumentNullException(nameof(pair));

        string topic = $"{TopicType.Ticker}:{pair}";
        return Subscribe(topic);
    }

    public IAsyncEnumerable<MessagePair> SubscribeTradesRaw(string pair)
    {
        if (pair == null) throw new ArgumentNullException(nameof(pair));

        string topic = $"{TopicType.Trades}:{pair}";
        return Subscribe(topic);
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

    private async Task Listen()
    {
        while (true)
        {
            try
            {
                var msgStr = await ReadNextMessage().ConfigureAwait(false);
                _logger?.LogDebug("Received \"{Msg}\"", msgStr);

                var message = JsonSerializer.Deserialize<Message>(msgStr, _serializerOptions)!;
                AsyncProducerConsumerQueue<MessagePair>? messages;
                lock (_topicToReceiver)
                {
                    _topicToReceiver.TryGetValue(message.Topic, out messages);
                }

                if (messages != null)
                    await messages.EnqueueAsync(new MessagePair { Message = message, RawMessage = msgStr });
            }
            catch (JsonException ex)
            {
                _logger?.LogError(ex, "Unable to parse message");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error reading message");
                break;
            }
        }
    }

    private async Task DoHeartbeat()
    {
        try
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(45)).ConfigureAwait(false);
                
                _logger?.LogDebug("Sending heartbeat");
                await _webSocket.SendString(CreateHeartbeatMsg());
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Heartbeat failed");
        }
    }

    private async Task<string> ReadNextMessage()
    {
        WebSocketReceiveResult result;
        var sb = new StringBuilder();

        do
        {
            result = await _webSocket.ReceiveAsync(_buffer, CancellationToken.None);
            var str = Encoding.Default.GetString(_buffer.Slice(0, result.Count));
            sb.Append(str);
        } while (!result.EndOfMessage);

        return sb.ToString();
    }
}