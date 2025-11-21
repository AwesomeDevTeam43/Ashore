using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Writes diagnostic events to both the Unity console and a persistent log file so that
/// release builds can be inspected after a scene transition fails.
/// </summary>
public static class BuildDiagnosticsLogger
{
    private const string LogFileName = "BuildDiagnostics.log";
    private static bool _initialized;
    private static string _logFilePath;

    private static string LogFilePath
    {
        get
        {
            if (!string.IsNullOrEmpty(_logFilePath))
            {
                return _logFilePath;
            }

            string folder = Application.isEditor
                ? Path.Combine(Application.dataPath, "../TempLogs")
                : Application.persistentDataPath;

            try
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"BuildDiagnosticsLogger: Failed to create log directory '{folder}': {ex.Message}");
            }

            _logFilePath = Path.Combine(folder, LogFileName);
            return _logFilePath;
        }
    }

    private static void EnsureSessionHeader()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        try
        {
            File.AppendAllText(LogFilePath, $"\n==== Build Session {DateTime.Now:yyyy-MM-dd HH:mm:ss} ====\n");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"BuildDiagnosticsLogger: Failed to write session header: {ex.Message}");
        }
    }

    public static void Log(string category, string message, UnityEngine.Object context = null)
    {
        Write("INFO", category, message);
        Debug.Log(FormatConsoleMessage("INFO", category, message), context);
    }

    public static void Warn(string category, string message, UnityEngine.Object context = null)
    {
        Write("WARN", category, message);
        Debug.LogWarning(FormatConsoleMessage("WARN", category, message), context);
    }

    public static void Error(string category, string message, UnityEngine.Object context = null)
    {
        Write("ERROR", category, message);
        Debug.LogError(FormatConsoleMessage("ERROR", category, message), context);
    }

    private static void Write(string level, string category, string message)
    {
        EnsureSessionHeader();
        try
        {
            File.AppendAllText(LogFilePath, $"[{DateTime.Now:HH:mm:ss}] {level} {category}: {message}\n");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"BuildDiagnosticsLogger: Failed to write log entry: {ex.Message}");
        }
    }

    private static string FormatConsoleMessage(string level, string category, string message)
    {
        return $"[{level}] {category}: {message}";
    }
}
