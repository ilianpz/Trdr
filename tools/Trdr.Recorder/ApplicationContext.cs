using Trdr.App;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Trdr.Recorder;

internal sealed class ApplicationContext
{
    public int ReturnCode { get; set; }

    public static ILogger CreateLogger(string categoryName, bool logFile, string? filePath)
    {
        var config = Application.CreateLoggerConfig().WithConsole();
        if (logFile)
        {
            config = config.WithFile(filePath);
        }

        return config.SetupLogger().CreateLogger(categoryName);
    }
}