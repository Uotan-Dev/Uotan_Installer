using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace UotanInstaller.App.Services;

/// <summary>
/// <para>安装日志服务实现，支持文件日志和内存日志双写，以及诊断信息导出。</para>
/// Installation log service implementation with file and memory dual-write logging and diagnostic information export.
/// </summary>
public sealed class InstallLogger : IInstallLogger, IDisposable
{
    private readonly ConcurrentBag<LogEntry> _memoryLog = [];
    private StreamWriter? _writer;
    private string? _logFilePath;
    private bool _disposed;

    private sealed record LogEntry(DateTimeOffset Timestamp, string Level, string StepName, string Message, string? ExceptionDetail);

    /// <inheritdoc/>
    public void Initialize(string logDirectory)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Directory.CreateDirectory(logDirectory);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss");
        _logFilePath = Path.Combine(logDirectory, $"install_{timestamp}.log");
        _writer = new StreamWriter(_logFilePath, false, Encoding.UTF8) { AutoFlush = false };
    }

    /// <inheritdoc/>
    public void LogInformation(string stepName, string message) => WriteLog("INFO", stepName, message, null);

    /// <inheritdoc/>
    public void LogWarning(string stepName, string message) => WriteLog("WARN", stepName, message, null);

    /// <inheritdoc/>
    public void LogError(string stepName, string message, Exception? exception = null) =>
        WriteLog("ERROR", stepName, message, exception?.ToString());

    /// <inheritdoc/>
    public async Task FlushAsync()
    {
        if (_writer is not null)
            await _writer.FlushAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ExportDiagnosticsAsync(string outputPath)
    {
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        using var zip = ZipFile.Open(outputPath, ZipArchiveMode.Create);

        if (_logFilePath is not null && File.Exists(_logFilePath))
            zip.CreateEntryFromFile(_logFilePath, Path.GetFileName(_logFilePath));

        var systemInfo = BuildSystemInfo();
        var entry = zip.CreateEntry("system_info.txt");
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, Encoding.UTF8);
        await writer.WriteAsync(systemInfo).ConfigureAwait(false);

        var memoryLogEntry = zip.CreateEntry("memory_log.txt");
        using var memStream = memoryLogEntry.Open();
        using var memWriter = new StreamWriter(memStream, Encoding.UTF8);
        foreach (var log in _memoryLog)
        {
            var line = FormatLogEntry(log.Timestamp, log.Level, log.StepName, log.Message, log.ExceptionDetail);
            await memWriter.WriteLineAsync(line).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// <para>释放日志服务占用的资源。</para>
    /// Releases the resources used by the log service.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _writer?.Flush();
        _writer?.Dispose();
    }

    private void WriteLog(string level, string stepName, string message, string? exceptionDetail)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var entry = new LogEntry(timestamp, level, stepName, message, exceptionDetail);
        _memoryLog.Add(entry);

        var line = FormatLogEntry(timestamp, level, stepName, message, exceptionDetail);
        if (_writer is not null)
        {
            _writer.WriteLine(line);
        }
        else
        {
            Debug.WriteLine(line);
        }
    }

    private static string FormatLogEntry(DateTimeOffset timestamp, string level, string stepName, string message, string? exceptionDetail)
    {
        var sb = new StringBuilder();
        sb.Append(timestamp.ToString("O"));
        sb.Append(" [").Append(level).Append("] ");
        sb.Append('[').Append(stepName).Append("] ");
        sb.Append(message);
        if (exceptionDetail is not null)
        {
            sb.AppendLine();
            sb.Append(exceptionDetail);
        }
        return sb.ToString();
    }

    private static string BuildSystemInfo()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"OS: {Environment.OSVersion}");
        sb.AppendLine($"Architecture: {System.Runtime.InteropServices.RuntimeInformation.OSArchitecture}");
        sb.AppendLine($"Framework: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
        sb.AppendLine($"MachineName: {Environment.MachineName}");
        sb.AppendLine($"ProcessorCount: {Environment.ProcessorCount}");
        sb.AppendLine($"Timestamp: {DateTimeOffset.UtcNow:O}");
        return sb.ToString();
    }
}
