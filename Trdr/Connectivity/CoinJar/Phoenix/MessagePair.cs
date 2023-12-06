namespace Trdr.Connectivity.CoinJar.Phoenix;

public readonly struct MessagePair
{
    public required Message Message { get; init; }
    public required string RawMessage { get; init; }
}