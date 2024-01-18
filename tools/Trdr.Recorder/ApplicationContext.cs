using Trdr.App;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Trdr.Recorder;

internal sealed class ApplicationContext
{
    public int ReturnCode { get; set; }

    public static ILogger CreateLogger(string categoryName, string? filePath)
    {
        if (categoryName == null) throw new ArgumentNullException(nameof(categoryName));
        var config = Application.CreateLoggerConfig().WithConsole();

        if (filePath != null)
            config = config.WithFile(filePath);

        return config.SetupLogger().CreateLogger(categoryName);
    }
}