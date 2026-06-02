using System;

namespace AudioAnalyzer.Interfaces;

public interface ILoggerService
{
    void Log(string message);
    void ClearLog();
}
