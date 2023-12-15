using System.Globalization;
using System.Text.Json.Serialization;

namespace Trdr.Connectivity.CoinJar.Phoenix;

public sealed class TickerPayload
{
    [JsonInclude]
    [JsonPropertyName("volume")]
    public string Volume { get; init; } = null!;

    [JsonInclude]
    [JsonPropertyName("transition_time")]
    public DateTime TransitionTime { get; init; }

    [JsonInclude]
    [JsonPropertyName("status")]
    public string Status { get; init; } = null!;
    
    [JsonInclude]
    [JsonPropertyName("session")]
    public long Session { get; init; }

    [JsonInclude]
    [JsonPropertyName("prev_close")]
    public string PreviousClose { get; init; } = null!;

    [JsonInclude]
    [JsonPropertyName("last")]
    public string Last { get; private init; } = null!;
    
    [JsonInclude]
    [JsonPropertyName("current_time")]
    public DateTime CurrentTime { get; init; }
    
    [JsonInclude]
    [JsonPropertyName("bid")]
    public string BidRaw { get; init; } = null!;

    public decimal Bid => decimal.Parse(BidRaw, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);

    [JsonInclude]
    [JsonPropertyName("ask")]
    public string AskRaw { get; init; } = null!;

    public decimal Ask => decimal.Parse(AskRaw, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
}