using System.Text.Json.Serialization;

namespace Trdr.Connectivity.CoinJar.Phoenix;

public sealed class ReplyPayload
{
    [JsonInclude]
    [JsonPropertyName("status")]
    public string Status { get; private set; } = null!;
}