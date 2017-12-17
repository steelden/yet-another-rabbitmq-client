using System;
using System.Globalization;
using EasyNetQ;
using NLog;

namespace YARC.Utility
{
    internal sealed class NLogEasyLogger : IEasyNetQLogger
    {
        private const string _loggerName = "EasyNetQ";
        private static Logger _logger = LogManager.GetLogger(_loggerName);

        private void Log(LogLevel level, Exception ex, string format, params object[] args)
        {
            var logEvent = new LogEventInfo(level, _loggerName, CultureInfo.CurrentCulture, format, args)
            {
                Exception = ex
            };
            _logger.Log(typeof(NLogEasyLogger), logEvent);
        }

        public void DebugWrite(string format, params object[] args)
        {
            Log(LogLevel.Trace, null, format, args);
        }

        public void ErrorWrite(Exception exception)
        {
            Log(LogLevel.Error, exception, "");
        }

        public void ErrorWrite(string format, params object[] args)
        {
            Log(LogLevel.Error, null, format, args);
        }

        public void InfoWrite(string format, params object[] args)
        {
            Log(LogLevel.Info, null, format, args);
        }
    }
}
