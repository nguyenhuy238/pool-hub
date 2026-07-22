namespace PoolHub.API.Extensions;

public static class LoggingExtensions
{
    public static ILoggingBuilder AddPoolHubLogging(this ILoggingBuilder logging)
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddDebug();

        return logging;
    }
}
