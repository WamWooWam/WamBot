using System;
using Microsoft.Extensions.Logging;
#if UI
using WamBotRewrite.UI;
#endif
namespace WamBotRewrite.Data
{
    internal class UILoggerFactory : ILoggerFactory
    {
        public void AddProvider(ILoggerProvider provider)
        {

        }

        public ILogger CreateLogger(string categoryName)
        {
            return new UILogger();
        }

        public void Dispose()
        {

        }
    }

    internal class UILogger : ILogger
    {
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
#if UI
            App.Current.Dispatcher.Invoke(() =>
            {
                ((MainWindow)App.Current.MainWindow).databaseLog.AppendText(formatter(state, exception) + "\r\n");
            });
#else
            Console.WriteLine(formatter(state, exception));
#endif
        }
    }
}