using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Trdr.Async;

namespace Trdr.Connectivity;

public abstract class WebSocketConnection
{
    private readonly string _connectionStr;

    private readonly ClientWebSocket _webSocket = new();
    private readonly ArraySegment<byte> _buffer = new(new byte[2048]);

    protected WebSocketConnection(string connectionStr, ILogger logger)
    {
        _connectionStr = connectionStr ?? throw new ArgumentNullException(nameof(connectionStr));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected ILogger Logger { get; }
    protected ClientWebSocket WebSocket => _webSocket;

    public async Task Connect(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Connecting...");

        await _webSocket.ConnectAsync(new Uri(_connectionStr), cancellationToken).ConfigureAwait(false);

        Listen().Forget();
        OnConnected();

        Logger.LogInformation("Connected");
    }

    public Task Disconnect(CancellationToken cancellationToken = default)
    {
        return _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal disconnect", cancellationToken);
    }

    protected virtual void OnConnected()
    {
    }

    protected abstract Task OnMessageReceived(string msgStr);

    private async Task Listen()
    {
        while (true)
        {
            try
            {
                var msgStr = await ReadNextMessage().ConfigureAwait(false);
                Logger.LogDebug("Received \"{Msg}\"", msgStr);

                await OnMessageReceived(msgStr);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error reading message");
                break;
            }
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