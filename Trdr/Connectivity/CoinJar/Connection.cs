using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nito.AsyncEx;

namespace Trdr.Connectivity.CoinJar
{
    public class Connection
    {
        private readonly ILogger<Connection>? _logger;

        private readonly ClientWebSocket _webSocket = new();
        private readonly string _connectionStr;
        private readonly ArraySegment<byte> _buffer = new(new byte[2048]);
        private readonly Dictionary<string, AsyncProducerConsumerQueue<string>> _topicToReceiver = new();

        private long _ref;

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

        public IAsyncEnumerable<string> Subscribe(string channel)
        {
            return Subscribe(channel, CancellationToken.None);
        }

        public async IAsyncEnumerable<string> Subscribe(
            string channel,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var request = CreateJoinRequest(channel);

            var receiver = new AsyncProducerConsumerQueue<string>();
            lock (_topicToReceiver)
            {
                _topicToReceiver[channel] = receiver;
            }

            await _webSocket.SendString(request).ConfigureAwait(false);

            while (true)
            {
                yield return await receiver.DequeueAsync(cancellationToken);
            }
        }

        public IAsyncEnumerable<string> SubscribeTicker(string pair)
        {
            if (pair == null) throw new ArgumentNullException(nameof(pair));

            string topic = $"ticker:{pair}";
            return Subscribe(topic);
        }

        public IAsyncEnumerable<string> SubscribeTrades(string pair)
        {
            if (pair == null) throw new ArgumentNullException(nameof(pair));

            string topic = $"trades:{pair}";
            return Subscribe(topic);
        }

        private static string CreateRequest(string topic, string @event, long @ref)
        {
            return $"{{ \"topic\": \"{topic}\", \"event\": \"{@event}\", \"payload\": {{}}, \"ref\": {@ref} }}";
        }

        private string CreateJoinRequest(string topic)
        {
            return CreateRequest(topic, "phx_join", GetRef());
        }

        private string CreateHeartbeatMsg()
        {
            return CreateRequest("phoenix", "heartbeat", GetRef());
        }

        private long GetRef() => Interlocked.Increment(ref _ref);


        private async Task Listen()
        {
            while (true)
            {
                try
                {
                    var msg = await ReadNextMessage().ConfigureAwait(false);
                    _logger?.LogDebug("Received \"{msg}\"", msg);

                    var json = JObject.Parse(msg);
                    var topic = json["topic"]!.ToString();

                    AsyncProducerConsumerQueue<string>? messages;
                    lock (_topicToReceiver)
                    {
                        _topicToReceiver.TryGetValue(topic, out messages);
                    }

                    if (messages != null)
                        await messages.EnqueueAsync(msg);
                }
                catch (JsonReaderException ex)
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
}
