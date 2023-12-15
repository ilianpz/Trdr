using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Enrichers;
using Serilog.Enrichers.ShortTypeName;

namespace Trdr.App
{
    public static class Application
    {
        private const string OutputTemplate = "{Timestamp:HH:mm:ss.ffffff} [{Level:u3}] [{ThreadId}] [{ShortTypeName}] {Message:lj}{NewLine}{Exception}";

        public static ILoggerFactory SetupDefaultLogger()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.With(new ShortTypeNameEnricher(), new ThreadIdEnricher())
                .WriteTo.File("log-.txt", outputTemplate: OutputTemplate, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var loggerFactory = new LoggerFactory()
                .AddSerilog(dispose: true);

            Module.WithLoggerFactory(loggerFactory);
            return loggerFactory;
        }
    }
}