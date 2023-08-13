using Microsoft.Extensions.Logging;

namespace TF.EX.Common.Extensions
{
    public static class LoggerExtensions
    {
        private static void Log<T>(this ILogger logger, LogLevel logLevel, string message, Exception exception = null)
        {
            logger.Log(logLevel, default, $"[{typeof(T).Name}] {message}", exception, Formatter);
        }

        public static void LogDebug<T>(this ILogger logger, string message)
        {
            Log<T>(logger, LogLevel.Debug, $"{message}", null);
        }

        public static void LogError<T>(this ILogger logger, string message, Exception exception = null)
        {
            Log<T>(logger, LogLevel.Error, $"{message}", exception);
        }

        private static string Formatter(string state, Exception exception)
        {
            return !string.IsNullOrEmpty(state) && exception != null ? $"{state} : {exception.Message}" : state;
        }
    }
}
