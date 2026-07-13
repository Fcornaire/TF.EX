//using Microsoft.Extensions.Logging;
//namespace TF.EX.Common.Logging
//{
//    public class Logger : ILogger
//    {
//        public static bool ShouldIgnoreCommandLog = false;

//        public IDisposable BeginScope<TState>(TState state) where TState : notnull => default!;

//        public bool IsEnabled(LogLevel logLevel)
//        {
//            return FortRise.RiseCore.DebugMode;
//        }

//        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
//        {
//            string message = formatter(state, exception);

//            ShouldIgnoreCommandLog = true;
//            FortRise.Logger.Log($"{message}", ToFortRiseLogLevel(logLevel));
//            ShouldIgnoreCommandLog = false;
//        }

//        private FortRise.Logger.LogLevel ToFortRiseLogLevel(LogLevel logLevel)
//        {
//            switch (logLevel)
//            {
//                case LogLevel.Debug:
//                    return FortRise.Logger.LogLevel.Debug;
//                case LogLevel.Information:
//                    return FortRise.Logger.LogLevel.Info;
//                case LogLevel.Warning:
//                    return FortRise.Logger.LogLevel.Warning;
//                case LogLevel.Error:
//                    return FortRise.Logger.LogLevel.Error;
//                default:
//                    return FortRise.Logger.LogLevel.Debug;
//            }
//        }
//    }
//}
