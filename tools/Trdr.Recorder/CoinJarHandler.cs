using System.CommandLine;
using Microsoft.Extensions.Logging;
using Trdr.Async;
using Trdr.Connectivity.CoinJar;
using Trdr.Connectivity.CoinJar.Phoenix;

namespace Trdr.Recorder;

public sealed class CoinJarHandler
{
    private readonly ILogger _logger;
    private List<string> _channels = new();

    public CoinJarHandler(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    internal Command GetCommand()
    {
        var coinJarCmd = new Command("coinjar");
        var channelOption = new Option<IEnumerable<string>>("--channels") { AllowMultipleArgumentsPerToken = true };
        coinJarCmd.AddOption(channelOption);
        coinJarCmd.SetHandler(
            (channelsValue) =>
            {
                _channels = channelsValue.ToList();
            },
            channelOption);

        return coinJarCmd;
    }

    public async Task<int?> Execute()
    {
        try
        {
            if (_channels.Count == 0)
                return null;

            _logger.LogInformation("Connecting...");
            Connection connection = Connection.Create();
            await connection.Connect();
            _logger.LogInformation("Connected");

            ReadChannels(connection);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex}");
            _logger.LogError(ex, "Error encountered");
            return 1;
        }
    }

    private void ReadChannels(Connection connection)
    {
        foreach (var channel in _channels)
        {
            Task.Run(async () =>
            {
                try
                {
                    await foreach (MessagePair pair in connection.Subscribe(channel))
                    {
                        _logger.LogInformation("[{Channel}] {RawMessage}", channel, pair.RawMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[{Channel}] Error from channel", channel);
                }
            }).Forget();
        }
    }
}