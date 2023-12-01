using System.Text.Json.Serialization;

namespace Trdr.Connectivity.CoinJar.Phoenix;

public sealed class MessageWithPayload<T> : Message
{
    [JsonInclude]
    [JsonPropertyName("payload")]
    public T Payload { get; private set; } = default!;
}