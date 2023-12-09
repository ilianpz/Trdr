using System.Text.Json.Serialization;

namespace Trdr.Connectivity.Binance;

public class StreamMessage
{
    public const string EventTypeProperty = "e";

    [JsonInclude]
    [JsonPropertyName(EventTypeProperty)]
    public string EventType { get; private set; } = null!;

    [JsonInclude]
    [JsonPropertyName("E")]
    public long EventTime { get; private set; }
}