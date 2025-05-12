using NLog;
using System;

namespace DatabaseHelper.Logging
{
    /// <summary>
    /// Implementation of ILogger using NLog framework
    /// </summary>
    public class NLogLogger : ILogger
    {
        private readonly Logger _logger;

        public NLogLogger(string name)
        {
            _logger = LogManager.GetLogger(name);
        }

        public NLogLogger(Type type)
        {
            _logger = LogManager.GetLogger(type.FullName);
        }

        public void Trace(string message)
        {
            _logger.Trace(message);
        }

        public void Trace(string message, Exception exception)
        {
            _logger.Trace(message, exception);
        }

        public void Debug(string message)
        {
            _logger.Debug(message);
        }

        public void Debug(string message, Exception exception)
        {
            _logger.Debug(exception, message);
        }

        public void Info(string message)
        {
            _logger.Info(message);
        }

        public void Info(string message, Exception exception)
        {
            _logger.Info(exception, message);
        }

        public void Warn(string message)
        {
            _logger.Warn(message);
        }

        public void Warn(string message, Exception exception)
        {
            _logger.Warn(exception, message);
        }

        public void Error(string message)
        {
            _logger.Error(message);
        }

        public void Error(string message, Exception exception)
        {
            _logger.Error(exception, message);
        }

        public void Fatal(string message)
        {
            _logger.Fatal(message);
        }

        public void Fatal(string message, Exception exception)
        {
            _logger.Fatal(exception, message);
        }
    }
}