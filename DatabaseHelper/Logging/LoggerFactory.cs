using System;

namespace DatabaseHelper.Logging
{
    /// <summary>
    /// Factory class for creating logger instances
    /// </summary>
    public static class LoggerFactory
    {
        /// <summary>
        /// Creates a logger for the specified type
        /// </summary>
        /// <typeparam name="T">The type to create the logger for</typeparam>
        /// <returns>A logger instance</returns>
        public static ILogger CreateLogger<T>()
        {
            return new NLogLogger(typeof(T));
        }

        /// <summary>
        /// Creates a logger for the specified type
        /// </summary>
        /// <param name="type">The type to create the logger for</param>
        /// <returns>A logger instance</returns>
        public static ILogger CreateLogger(Type type)
        {
            return new NLogLogger(type);
        }

        /// <summary>
        /// Creates a logger with the specified name
        /// </summary>
        /// <param name="name">The name of the logger</param>
        /// <returns>A logger instance</returns>
        public static ILogger CreateLogger(string name)
        {
            return new NLogLogger(name);
        }
    }
}