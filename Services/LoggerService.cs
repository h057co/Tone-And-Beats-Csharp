using System;
using System.IO;
using AudioAnalyzer.Interfaces;

namespace AudioAnalyzer.Services;

public class LoggerService : ILoggerService
{
    private readonly object _lock = new object();
    private StreamWriter? _writer;

    private readonly string _logFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ToneAndBeats",
        "app.log");

    public static ILoggerService Instance { get; set; } = new LoggerService();

    // Instance implementation
    void ILoggerService.Log(string message) => LogInstance(message);
    void ILoggerService.ClearLog() => ClearLogInstance();

    private void LogInstance(string message)
    {
        lock (_lock)
        {
            try
            {
                if (_writer == null)
                {
                    var directory = Path.GetDirectoryName(_logFilePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        Directory.CreateDirectory(directory);
                    _writer = new StreamWriter(_logFilePath, append: true) { AutoFlush = true };
                }

                _writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoggerService.Log failed: {ex.Message}");
            }
        }
    }

    private void ClearLogInstance()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_logFilePath))
                    File.Delete(_logFilePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoggerService.ClearLog failed: {ex.Message}");
            }
        }
    }

    // Static wrapper for backward compatibility (100+ calls)
    public static void Log(string message)
    {
        if (Instance is LoggerService concrete)
        {
            concrete.LogInstance(message);
        }
        else
        {
            Instance.Log(message);
        }
    }

    public static void ClearLog()
    {
        if (Instance is LoggerService concrete)
        {
            concrete.ClearLogInstance();
        }
        else
        {
            Instance.ClearLog();
        }
    }
}