namespace Trdr.Connectivity.Binance;

public sealed class Connection : WebSocketConnection
{
    private Connection(string connectionStr)
        : base(connectionStr, Module.CreateLogger<Connection>()!)
    {
    }

    public static Connection Create()
    {
        return new Connection("wss://stream.binance.com:443");
    }

#pragma warning disable CS1998 // Rider complains about "useless" async
    public async IAsyncEnumerable<Ticker> SubscribeTicker(string symbol)
    {
        yield break;
    }
#pragma warning restore CS1998 // Rider complains about "useless" async

    protected override Task OnMessageReceived(string msgStr)
    {
        throw new NotImplementedException();
    }
}