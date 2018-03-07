using ConsoleDraw.UI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace WamBot.Data
{
    public class LoggerFactory : ILoggerFactory
    {
        static TextArea _area;

        internal static void Setup(TextArea area)
        {
            _area = area;
        }

        public void AddProvider(ILoggerProvider provider)
        {

        }

        public ILogger CreateLogger(string categoryName)
        {
            return new Logger(categoryName, _area);
        }

        public void Dispose()
        {

        }
    }

    internal class Logger : ILogger
    {
        private TextArea _area;
        private string _category;

        public Logger(string category, TextArea area)
        {
            _category = category;
            _area = area;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (formatter != null)
            {
                string message = formatter(state, exception);
                _area?.WriteLine($"{_category}: {message}");
            }
        }
    }
}
