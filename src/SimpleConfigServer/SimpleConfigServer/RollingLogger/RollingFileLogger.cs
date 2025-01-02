namespace SimpleConfigServer.RollingLogger
{
    public class RollingFileLogger
    {

        private readonly string _logDirectory;
        private readonly string _baseFileName;
        private readonly int _maxFileCount;
        private readonly object _lock = new();
        private string _currentLogFile;

        public RollingFileLogger(string logDirectory, string baseFileName, int maxFileCount = 10)
        {
            _logDirectory = logDirectory;
            _baseFileName = baseFileName;
            _maxFileCount = maxFileCount;

            _currentLogFile = GetCurrentLogFileName();

            CleanUpOldFiles();
        }

        private string GetCurrentLogFileName()
        {
            return Path.Combine(_logDirectory, $"{_baseFileName}_{DateTime.Now:yyyy-MM-dd}.log");
        }

        /// <summary>
        /// Deletes the oldest files and only keeps the latest 10.
        /// </summary>
        private void CleanUpOldFiles()
        {
            var logfiles = Directory.GetFiles(_logDirectory, $"{_baseFileName}_*.log")
                .OrderByDescending( f => f )
                .Skip( _maxFileCount )
                .ToList();

            foreach (var logfile in logfiles)
            {
                try
                {
                    File.Delete(logfile);
                }
                catch (Exception)
                {
                    // skip and try next time
                }
            }
        }

        public void Log(string message)
        {
            lock (_lock)
            {
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd_HH-mm-ss.ff}] {message}{Environment.NewLine}";
                var newLogFile = GetCurrentLogFileName();

                //check whether new log file is necessary
                if (newLogFile != _currentLogFile)
                {
                    _currentLogFile = newLogFile;
                    CleanUpOldFiles();
                }

                File.AppendAllText(newLogFile, logEntry);
            }
        }
    }
}
