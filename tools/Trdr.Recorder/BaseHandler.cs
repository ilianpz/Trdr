﻿using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.Logging;

namespace Trdr.Recorder;

internal static class BaseHandler
{
    public static async Task<int> Handle(
        string logContext,
        Func<ILogger, Task> handler, InvocationContext invocationContext, Option<string> logOption)
    {
        ILogger logger = ApplicationContext.CreateLogger(
            logContext,
            invocationContext.ParseResult.GetValueForOption(logOption));

        try
        {
            await handler(logger);
            return 0;
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Operation cancelled");
            return -1;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error");
            return -2;
        }
    }
}