namespace Trdr.Connectivity.CoinJar.Phoenix;

public readonly struct MessagePair
{
    public Message Message { get; init; }
    public string RawMessage { get; init; }
}