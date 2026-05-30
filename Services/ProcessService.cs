using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace UotanInstaller.App.Services;

/// <summary>
/// <para>进程管理服务实现，提供进程查询、终止和启动功能</para>
/// Process management service implementation that provides process query, termination, and launch functionality
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
                // Ignore access denied or process already exited
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
            // Process already exited
        }
        catch (Win32Exception)
        {
            // Access denied or process already terminated
        }
    }

    /// <inheritdoc/>
    public int WaitForProcessExit(int pid)
    {
        using var process = Process.GetProcessById(pid);
        process.WaitForExit();
        return process.ExitCode;
    }

    /// <inheritdoc/>
    public void RunElevated(string programPath, string? args = null)
    {
        RunShellExecute(programPath, args, elevated: true);
    }

    /// <inheritdoc/>
    public void RunNormal(string programPath, string? args = null)
    {
        try
        {
            RunShellExecute(programPath, args, elevated: false);
            return;
        }
        catch
        {
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = programPath,
                UseShellExecute = true,
                Arguments = args ?? string.Empty,
            };
            Process.Start(startInfo)?.Dispose();
        }
        catch
        {
        }
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

    private static void RunShellExecute(string programPath, string? args, bool elevated)
    {
        var sei = new SHELLEXECUTEINFOW
        {
            cbSize = (uint)Marshal.SizeOf<SHELLEXECUTEINFOW>(),
            fMask = SEE_MASK_NOCLOSEPROCESS,
            lpVerb = elevated ? "runas" : "open",
            lpFile = programPath,
            lpParameters = args ?? string.Empty,
            nShow = SW_SHOWNORMAL,
        };

        if (!ShellExecuteExW(ref sei))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        if (sei.hProcess != IntPtr.Zero)
        {
            CloseHandle(sei.hProcess);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHELLEXECUTEINFOW
    {
        public uint cbSize;
        public uint fMask;
        public string? lpVerb;
        public string? lpFile;
        public string? lpParameters;
        public string? lpDirectory;
        public int nShow;
        public IntPtr hInstApp;
        public IntPtr hProcess;
    }

    private const uint SEE_MASK_NOCLOSEPROCESS = 0x00000040;
    private const int SW_SHOWNORMAL = 1;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShellExecuteExW(ref SHELLEXECUTEINFOW pExecInfo);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);
}
