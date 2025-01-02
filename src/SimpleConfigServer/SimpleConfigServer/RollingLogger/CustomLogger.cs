namespace SimpleConfigServer.RollingLogger
{
    public class CustomLogger : ILogger
    {
        private readonly RollingFileLogger _fileLogger;
        private readonly string _categoryName;

        public CustomLogger(RollingFileLogger fileLogger, string categoryName)
        {
            _fileLogger = fileLogger;
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) => default;

        public bool IsEnabled(LogLevel level) => true;

        public void Log<TState>(LogLevel logLevel
            , EventId eventId
            , TState state
            , Exception? exception
            , Func<TState, Exception?, string> formatter
            )
        {
            var message = formatter(state, exception);
            var logMessage = $"[{logLevel}] {_categoryName}: {message}";

            if (exception != null)
                logMessage += $"{Environment.NewLine}Exception: {exception}";

            _fileLogger.Log(logMessage);
        }
    }
}
