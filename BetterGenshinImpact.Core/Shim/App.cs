using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BetterGenshinImpact;

public static class App
{
    private static ILoggerFactory? _factory;

    public static void Initialize(ILoggerFactory factory) => _factory = factory;

    public static ILogger<T> GetLogger<T>() =>
        (_factory ?? NullLoggerFactory.Instance).CreateLogger<T>();

    public static IServiceProvider ServiceProvider =>
        throw new NotSupportedException("App.ServiceProvider not available in Core.");
}
