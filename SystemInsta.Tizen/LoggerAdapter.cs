using Microsoft.Extensions.Logging;
using System;
using TizenLogger = global::Tizen.Log;

namespace SystemInsta.Tizen
{
    internal class LoggerAdapter<T> : ILogger<T>
    {
        private static readonly string Tag = typeof(T).Name;

        public void Log<TState>(
            LogLevel logLevel, 
            EventId eventId, 
            TState state, 
            Exception exception, 
            Func<TState, Exception, string> formatter)
        {
            if (logLevel == LogLevel.None)
            {
                return;
            }

            var formatted = formatter?.Invoke(state, exception)
                          ?? exception?.ToString()
                          ?? state?.ToString()
                          ?? eventId.ToString();

            switch (logLevel)
            {
                case LogLevel.Trace:
                    TizenLogger.Verbose(Tag, formatted);
                    break;
                case LogLevel.Debug:
                    TizenLogger.Debug(Tag, formatted);
                    break;
                case LogLevel.Information:
                    TizenLogger.Info(Tag, formatted);
                    break;
                case LogLevel.Warning:
                    TizenLogger.Warn(Tag, formatted);
                    break;
                case LogLevel.Error:
                    TizenLogger.Error(Tag, formatted);
                    break;
                case LogLevel.Critical:
                    TizenLogger.Fatal(Tag, formatted);
                    break;
                default:
                    break;
            }
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable BeginScope<TState>(TState state) => NoOpDisposable.Instance;

        private sealed class NoOpDisposable : IDisposable
        {
            internal static NoOpDisposable Instance { get; } = new NoOpDisposable();
            public void Dispose() { }
        }
    }
}
