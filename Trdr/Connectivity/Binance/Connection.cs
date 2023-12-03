namespace Trdr.Connectivity.Binance;

public sealed class Connection : WebSocketConnection
{
    private Connection(string connectionStr)
        : base(connectionStr, Module.CreateLogger<Connection>()!)
    {
    }

    protected override Task OnMessageReceived(string msgStr)
    {
        throw new NotImplementedException();
    }
}