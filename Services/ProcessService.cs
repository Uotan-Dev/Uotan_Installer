using System.ComponentModel;
using System.Diagnostics;

namespace UotanInstaller.App.Services;

/// <summary>
/// <para>进程管理服务实现，提供进程查询和终止功能</para>
/// Process management service implementation that provides process query and termination functionality
/// </summary>
public sealed class ProcessService : IProcessService
{
    /// <inheritdoc/>
    public (bool IsRunning, int? Pid) IsProcessRunning(string processName, string? pathPart = null)
    {
        var processes = Process.GetProcessesByName(
            Path.GetFileNameWithoutExtension(processName));

        foreach (var process in processes)
        {
            try
            {
                if (pathPart is null)
                {
                    process.Dispose();
                    return (true, process.Id);
                }

                var path = GetProcessPath(process.Id);
                if (path is not null && path.Contains(pathPart, StringComparison.OrdinalIgnoreCase))
                {
                    process.Dispose();
                    return (true, process.Id);
                }
            }
            catch
            {
            }
            finally
            {
                process.Dispose();
            }
        }

        return (false, null);
    }

    /// <inheritdoc/>
    public void KillProcess(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            process.Kill(true);
        }
        catch (ArgumentException)
        {
        }
        catch (Win32Exception)
        {
        }
    }

    /// <inheritdoc/>
    public int WaitForProcessExit(int pid)
    {
        using var process = Process.GetProcessById(pid);
        process.WaitForExit();
        return process.ExitCode;
    }

    private static string? GetProcessPath(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return process.MainModule?.FileName;
        }
        catch
        {
            return null;
        }
    }
}
