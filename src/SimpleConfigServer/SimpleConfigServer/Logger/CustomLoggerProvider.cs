namespace SimpleConfigServer.Logger
{
    public class CustomLoggerProvider : ILoggerProvider
    {
        private readonly RollingFileLogger _logger;

        public CustomLoggerProvider(string logDirectory)
        {
            _logger = new RollingFileLogger(logDirectory, "server");
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new CustomLogger(_logger, categoryName);
        }

        public void Dispose()
        {

        }
    }
}
