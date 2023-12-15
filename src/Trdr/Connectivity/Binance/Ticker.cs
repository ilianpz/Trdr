using System.Globalization;
using System.Text.Json.Serialization;

namespace Trdr.Connectivity.Binance;

public class Ticker
{
    [JsonInclude]
    [JsonPropertyName("u")]
    public long UpdateId { get; init; }

    [JsonInclude]
    [JsonPropertyName("s")]
    public string Symbol { get; init; } = null!;

    [JsonInclude]
    [JsonPropertyName("b")]
    public string BidRaw { get; init; } = null!;

    public decimal Bid => decimal.Parse(BidRaw, CultureInfo.InvariantCulture);

    [JsonInclude]
    [JsonPropertyName("B")]
    public string BidQtyRaw { get; init; } = null!;

    public decimal BidQty => decimal.Parse(BidQtyRaw, CultureInfo.InvariantCulture);

    [JsonInclude]
    [JsonPropertyName("a")]
    public string AskRaw { get; init; } = null!;

    public decimal Ask  => decimal.Parse(AskRaw, CultureInfo.InvariantCulture);

    [JsonInclude]
    [JsonPropertyName("A")]
    public string AskQtyRaw { get; init; } = null!;

    public decimal AskQty => decimal.Parse(AskQtyRaw, CultureInfo.InvariantCulture);
}