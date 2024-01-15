using System.Net.WebSockets;
using System.Text;

namespace Trdr.Connectivity
{
    public static class WebSocketExtensions
    {
        public static Task SendString(
            this ClientWebSocket webSocket, string msg,
            CancellationToken cancellationToken = default)
        {
            if (msg == null) throw new ArgumentNullException(nameof(msg));

            var bytes = Encoding.Default.GetBytes(msg);
            var segment = new ArraySegment<byte>(bytes);
            return webSocket.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
        }
    }
}