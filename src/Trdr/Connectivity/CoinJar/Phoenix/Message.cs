using System.Text.Json.Serialization;

namespace Trdr.Connectivity.CoinJar.Phoenix;

public class Message
{
    public const string TopicProperty = "topic";
    public const string EventProperty = "event";

    [JsonInclude]
    [JsonPropertyName(TopicProperty)]
    public required string Topic { get; init; }

    [JsonInclude]
    [JsonPropertyName("ref")]
    public long? Ref { get; private set; }

    [JsonInclude]
    [JsonPropertyName(EventProperty)]
    public required string Event { get; init; }
}