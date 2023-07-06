
using Microsoft.Extensions.Logging;

namespace Trdr
{
    public class Module
    {
        private static readonly Lazy<Module> _instance = new(() => new Module());
        private static ILoggerFactory? _loggerFactory;

        public static Module WithLoggerFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            return _instance.Value;
        }

        public static ILogger<T>? CreateLogger<T>()
        {
            return _loggerFactory?.CreateLogger<T>();
        }
    }
}
