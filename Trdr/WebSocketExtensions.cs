using System.Net.WebSockets;
using System.Text;

namespace Trdr
{
    public static class WebSocketExtensions
    {
        public static Task SendString(this ClientWebSocket webSocket, string msg)
        {
            if (msg == null) throw new ArgumentNullException(nameof(msg));

            var bytes = Encoding.Default.GetBytes(msg);
            var segment = new ArraySegment<byte>(bytes);
            return webSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
