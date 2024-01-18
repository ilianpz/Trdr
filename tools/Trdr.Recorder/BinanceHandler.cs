using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.Logging;
using Trdr.Connectivity.Binance;

namespace Trdr.Recorder;

internal sealed class BinanceHandler
{
    private readonly ApplicationContext _context;
    private readonly Option<string> _logOption;

    public BinanceHandler(ApplicationContext context, Option<string> logOption)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logOption = logOption ?? throw new ArgumentNullException(nameof(logOption));
    }

    internal Command GetCommand()
    {
        var binanceCmd = new Command("binance");
        var streamsArgument = new Argument<IEnumerable<string>>
        {
            Arity = ArgumentArity.OneOrMore,
            HelpName = "streams",
            Description = "The WebSocket streams to subscribe to."
        };
        binanceCmd.AddArgument(streamsArgument);
        binanceCmd.SetHandler(
            async invocationContext =>
            {
                var streams = invocationContext.ParseResult.GetValueForArgument(streamsArgument);
                var token = invocationContext.GetCancellationToken();
                _context.ReturnCode = await ReadStreams(streams, invocationContext, token);
            });

        return binanceCmd;
    }

    private Task<int> ReadStreams(
        IEnumerable<string> streams, InvocationContext invocationContext, CancellationToken cancellationToken)
    {
        return BaseHandler.Handle(
            nameof(BinanceHandler),
            async logger =>
            {
                logger.LogInformation("Connecting...");
                var readTasks =
                    streams.Select(stream =>
                        Task.Run(async () =>
                            {
                                await foreach (var timeStamped in
                                               Streams.GetStream(stream).ToAsyncEnumerable()
                                                   .WithCancellation(cancellationToken))
                                {
                                    logger.LogInformation("{RawMessage}", timeStamped.Value);
                                }
                            },
                            cancellationToken));

                await Task.WhenAll(readTasks);
            },
            invocationContext,
            _logOption);
    }
}