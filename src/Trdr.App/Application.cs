using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Enrichers;
using Serilog.Enrichers.ShortTypeName;

namespace Trdr.App
{
    public static class Application
    {
        private const string ConsoleTemplate = "{Message:lj}{NewLine}{Exception}";
        private const string FileTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.ffffff} [{Level:u3}] [{ThreadId}] [{ShortTypeName}] {Message:lj}{NewLine}{Exception}";

        public static LoggerConfiguration CreateLoggerConfig()
        {
            return new LoggerConfiguration()
                .Enrich.With(new ShortTypeNameEnricher(), new ThreadIdEnricher());
        }

        public static LoggerConfiguration WithConsole(this LoggerConfiguration config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            return config.WriteTo.Console(outputTemplate: ConsoleTemplate);
        }

        public static LoggerConfiguration WithFile(this LoggerConfiguration config, string filePath)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            return config.WriteTo.File(
                filePath, outputTemplate: FileTemplate, rollingInterval: RollingInterval.Day);
        }

        public static ILoggerFactory SetupLogger(this LoggerConfiguration config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            Log.Logger = config.CreateLogger();

            var loggerFactory = new LoggerFactory()
                .AddSerilog(dispose: true);

            return loggerFactory;
        }
    }
}