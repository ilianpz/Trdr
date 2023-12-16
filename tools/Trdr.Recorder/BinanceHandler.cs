using System.CommandLine;
using Microsoft.Extensions.Logging;
using Trdr.Async;
using Trdr.Connectivity.Binance;

namespace Trdr.Recorder;

public sealed class BinanceHandler
{
    private readonly ILogger _logger;
    private List<string> _streams = new();

    public BinanceHandler(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    internal Command GetCommand()
    {
        var binanceCmd = new Command("binance");

        var streamsOption = new Option<IEnumerable<string>>("--streams") { AllowMultipleArgumentsPerToken = true };
        binanceCmd.AddOption(streamsOption);
        binanceCmd.SetHandler(
            channelsValue =>
            {
                _streams = channelsValue.ToList();
            },
            streamsOption);

        return binanceCmd;
    }

    public int? Execute()
    {
        try
        {
            if (_streams.Count == 0)
                return null;

            ReadStreams();
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex}");
            _logger.LogError(ex, "Error encountered");
            return 1;
        }
    }

    private void ReadStreams()
    {
        foreach (var stream in _streams)
        {
            Task.Run(async () =>
            {
                try
                {
                    await foreach (var message in Streams.SubscribeRaw(stream))
                    {
                        _logger.LogInformation("[{Stream}] {RawMessage}", stream, message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[{Stream}] Error from stream", stream);
                }
            }).Forget();
        }
    }
}