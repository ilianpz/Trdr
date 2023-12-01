using System.Text.Json.Serialization;

namespace Trdr.Connectivity.CoinJar.Phoenix;

public class Message
{
    public const string TopicProperty = "topic";
    public const string EventProperty = "event";

    [JsonInclude]
    [JsonPropertyName(TopicProperty)]
    public string Topic { get; private set; } = null!;

    [JsonInclude]
    [JsonPropertyName("ref")]
    public long? Ref { get; private set; }

    [JsonInclude]
    [JsonPropertyName(EventProperty)]
    public string Event { get; private set; } = null!;
}