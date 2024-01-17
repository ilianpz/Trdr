using System.CommandLine;
using System.CommandLine.Invocation;
using System.Reactive;
using Microsoft.Extensions.Logging;
using Trdr.Connectivity.CoinJar;
using Trdr.Connectivity.CoinJar.Phoenix;

namespace Trdr.Recorder;

internal sealed class CoinJarHandler
{
    private readonly ApplicationContext _context;
    private readonly Option<string> _logOption;

    public CoinJarHandler(ApplicationContext context, Option<string> logOption)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logOption = logOption ?? throw new ArgumentNullException(nameof(logOption));
    }

    internal Command GetCommand()
    {
        var coinJarCmd = new Command("coinjar");
        var channelOption = new Option<IEnumerable<string>>("--channel") { AllowMultipleArgumentsPerToken = true };
        coinJarCmd.AddOption(channelOption);
        coinJarCmd.SetHandler(
            async invocationContext =>
            {
                var channels = invocationContext.ParseResult.GetValueForOption(channelOption);
                var token = invocationContext.GetCancellationToken();
                _context.ReturnCode = await ReadChannels(channels!, invocationContext, token);
            });

        return coinJarCmd;
    }

    private Task<int> ReadChannels(
        IEnumerable<string> channels, InvocationContext invocationContext, CancellationToken cancellationToken)
    {
        return BaseHandler.Handle(
            async logger =>
            {
                logger.LogInformation("Connecting...");
                Connection connection = Connection.Create();
                await connection.Connect(cancellationToken);
                logger.LogInformation("Connected");

                var tasks = channels.Select(channel =>
                    Task.Run(async () =>
                    {
                        await foreach (Timestamped<MessagePair> timeStamped in
                                       connection.GetChannel(channel)
                                           .ToAsyncEnumerable()
                                           .WithCancellation(cancellationToken))
                        {
                            logger.LogInformation(
                                "Timestamp:{Timestamp} Message:{RawMessage}",
                                timeStamped.Timestamp, timeStamped.Value.RawMessage);
                        }
                    }, cancellationToken));

                await Task.WhenAll(tasks);
            },
            invocationContext,
            _logOption);
    }
}