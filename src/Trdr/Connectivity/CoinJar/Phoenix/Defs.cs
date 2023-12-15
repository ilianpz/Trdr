namespace Trdr.Connectivity.CoinJar.Phoenix;

public static class EventType
{
    public const string HeartBeat = "heartbeat";
    public const string Join = "phx_join";
    public const string Reply = "phx_reply";
}

public static class TopicType
{
    public const string Book = "book";
    public const string Trades = "trades";
    public const string Ticker = "ticker";
    public const string Phoenix = "phoenix";
    public const string Private = "private";
}