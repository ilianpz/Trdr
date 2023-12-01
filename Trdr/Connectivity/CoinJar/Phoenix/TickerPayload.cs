using System.Text.Json.Serialization;

namespace Trdr.Connectivity.CoinJar.Phoenix;

public sealed class TickerPayload
{
    [JsonInclude]
    [JsonPropertyName("volume")]
    public string Volume { get; private set; } = null!;

    [JsonInclude]
    [JsonPropertyName("transition_time")]
    public DateTime TransitionTime { get; private set; }

    [JsonInclude]
    [JsonPropertyName("status")]
    public string Status { get; private set; } = null!;
    
    [JsonInclude]
    [JsonPropertyName("session")]
    public long Session { get; private set; }

    [JsonInclude]
    [JsonPropertyName("prev_close")]
    public string PreviousClose { get; private set; } = null!;

    [JsonInclude]
    [JsonPropertyName("last")]
    public string Last { get; private set; } = null!;
    
    [JsonInclude]
    [JsonPropertyName("current_time")]
    public DateTime CurrentTime { get; private set; }
    
    [JsonInclude]
    [JsonPropertyName("bid")]
    public string Bid { get; private set; } = null!;
    
    [JsonInclude]
    [JsonPropertyName("ask")]
    public string Ask { get; private set; } = null!;
}