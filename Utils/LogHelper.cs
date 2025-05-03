using System;
using System.IO;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace HwpToPdf.Utils
{
    /// <summary>
    /// 로깅 유틸리티 클래스
    /// </summary>
    public static class LogHelper
    {
        private static Logger _logger;

        public static void Initialize(LogLevel level = LogLevel.Info, string logFilePath = null)
        {
            var logConfig = new LoggerConfiguration()
                .MinimumLevel.Is(ConvertLogLevel(level))
                .WriteTo.Console();

            if (!string.IsNullOrEmpty(logFilePath))
            {
                var directory = Path.GetDirectoryName(logFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                logConfig = logConfig.WriteTo.File(
                    logFilePath,
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
            }

            _logger = logConfig.CreateLogger();
        }

        public static void LogError(string message, Exception exception = null)
        {
            EnsureInitialized();
            if (exception != null)
                _logger.Error(exception, message);
            else
                _logger.Error(message);
        }

        public static void LogWarning(string message)
        {
            EnsureInitialized();
            _logger.Warning(message);
        }

        public static void LogInfo(string message)
        {
            EnsureInitialized();
            _logger.Information(message);
        }

        public static void LogDebug(string message)
        {
            EnsureInitialized();
            _logger.Debug(message);
        }

        public static void LogVerbose(string message)
        {
            EnsureInitialized();
            _logger.Verbose(message);
        }

        private static void EnsureInitialized()
        {
            if (_logger == null)
                Initialize();
        }

        private static LogEventLevel ConvertLogLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Error => LogEventLevel.Error,
                LogLevel.Warning => LogEventLevel.Warning,
                LogLevel.Info => LogEventLevel.Information,
                LogLevel.Debug => LogEventLevel.Debug,
                LogLevel.Trace => LogEventLevel.Verbose,
                _ => LogEventLevel.Information
            };
        }
    }

    /// <summary>
    /// 로그 레벨
    /// </summary>
    public enum LogLevel
    {
        Error,
        Warning,
        Info,
        Debug,
        Trace
    }
} 