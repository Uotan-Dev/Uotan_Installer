using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace UotanInstaller.App.Services.Deployment.Platforms;

/// <summary>
/// <para>Windows 平台适配器实现，提供安装目录解析、快捷方式创建、应用启动及权限提升等 Windows 特定操作。</para>
/// Windows platform adapter implementation, providing Windows-specific operations such as install directory resolution, shortcut creation, application launching, and privilege elevation.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsPlatformAdapter : IPlatformAdapter
{
    /// <summary>
    /// <para>获取指定应用程序在 Windows 上的默认安装目录路径（Program Files 下）。</para>
    /// Gets the default installation directory path for the specified application under Program Files on Windows.
    /// </summary>
    /// <param name="appName">
    /// <para>应用程序名称。</para>
    /// The application name.
    /// </param>
    /// <returns>
    /// <para>默认安装目录的绝对路径。</para>
    /// The absolute path of the default installation directory.
    /// </returns>
    public string GetDefaultInstallDirectory(string appName)
    {
        var programFiles = Environment.GetEnvironmentVariable("ProgramW6432")
            ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        return Path.Combine(programFiles, appName);
    }

    /// <summary>
    /// <para>在 Windows 桌面创建应用程序快捷方式（.lnk 文件），使用 IWshRuntimeLibrary（Windows Script Host Object Model）COM 组件实现。</para>
    /// Creates an application shortcut (.lnk file) on the Windows desktop using the IWshRuntimeLibrary (Windows Script Host Object Model) COM component.
    /// </summary>
    /// <param name="appName">
    /// <para>应用程序名称，用作快捷方式文件名。</para>
    /// The application name, used as the shortcut file name.
    /// </param>
    /// <param name="targetPath">
    /// <para>快捷方式指向的目标可执行文件路径。</para>
    /// The target executable file path that the shortcut points to.
    /// </param>
    /// <param name="args">
    /// <para>启动目标程序时传递的命令行参数，可为 null。</para>
    /// The command-line arguments passed when launching the target program, or null.
    /// </param>
    /// <param name="ct">
    /// <para>取消令牌。</para>
    /// Cancellation token.
    /// </param>
    /// <returns>
    /// <para>表示异步操作的 Task。</para>
    /// A task representing the asynchronous operation.
    /// </returns>
    public Task CreateDesktopShortcutAsync(string appName, string targetPath, string? args = null, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var lnkPath = Path.Combine(desktopPath, $"{appName}.lnk");

            var shell = new IWshRuntimeLibrary.WshShell();
            var shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(lnkPath);
            shortcut.TargetPath = targetPath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath) ?? string.Empty;
            shortcut.WindowStyle = 1;
            if (!string.IsNullOrEmpty(args))
                shortcut.Arguments = args;
            shortcut.Save();
        }, ct);
    }

    /// <summary>
    /// <para>使用 ShellExecuteExW 以指定动词启动程序。</para>
    /// Launches a program using ShellExecuteExW with the specified verb.
    /// </summary>
    public void LaunchApplication(string exePath, string? args = null)
    {
        ShellExecute(exePath, args, "open");
    }

    /// <summary>
    /// <para>使用 ShellExecuteExW 以 "runas" 动词请求管理员权限启动指定程序。</para>
    /// Requests administrator privileges to launch the specified program using ShellExecuteExW with the "runas" verb.
    /// </summary>
    public void RequestElevation(string exePath, string? args = null)
    {
        ShellExecute(exePath, args, "runas");
    }

    /// <summary>
    /// <para>获取 Windows 平台的临时文件目录路径。</para>
    /// Gets the temporary file directory path on Windows.
    /// </summary>
    public string GetTempDirectory() => Path.GetTempPath();

    /// <summary>
    /// <para>在指定安装目录中查找应用程序的可执行文件路径。优先搜索匹配 "UotanToolbox*.exe" 模式的文件，若未找到则搜索目录下所有 .exe 文件。</para>
    /// Finds the application executable file path in the specified installation directory. Searches for files matching the "UotanToolbox*.exe" pattern first, then falls back to all .exe files in the directory.
    /// </summary>
    /// <param name="installPath">
    /// <para>安装目录路径。</para>
    /// The installation directory path.
    /// </param>
    /// <returns>
    /// <para>表示异步操作的任务，结果为可执行文件路径；若未找到则返回 null。</para>
    /// A task representing the asynchronous operation, with the result being the executable file path; or null if not found.
    /// </returns>
    public Task<string?> FindExecutableAsync(string installPath)
    {
        return Task.Run(() =>
        {
            if (!Directory.Exists(installPath)) return null;

            var exeFiles = Directory.GetFiles(installPath, "UotanToolbox*.exe", SearchOption.TopDirectoryOnly);
            if (exeFiles.Length > 0) return exeFiles[0];

            var allExeFiles = Directory.GetFiles(installPath, "*.exe", SearchOption.TopDirectoryOnly);
            return allExeFiles.Length > 0 ? allExeFiles[0] : null;
        });
    }

    /// <summary>
    /// <para>使用 Windows 资源管理器打开指定目录。</para>
    /// Opens the specified directory using Windows File Explorer.
    /// </summary>
    /// <param name="path">
    /// <para>要打开的目录路径。</para>
    /// The directory path to open.
    /// </param>
    /// <returns>
    /// <para>表示异步操作的 Task。</para>
    /// A task representing the asynchronous operation.
    /// </returns>
    public Task OpenInFileExplorerAsync(string path)
    {
        return Task.Run(() =>
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{path}\"",
                UseShellExecute = false,
            };
            System.Diagnostics.Process.Start(psi)?.Dispose();
        });
    }

    /// <summary>
    /// <para>异步注册 URL 协议处理器。</para>
    /// Asynchronously registers a URL protocol handler.
    /// </summary>
    public Task RegisterProtocolHandlerAsync(string scheme, string exePath, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($"SOFTWARE\\Classes\\{scheme}");
            key.SetValue(string.Empty, $"URL:{scheme} Protocol");
            key.SetValue("URL Protocol", string.Empty);
            var shellKey = key.CreateSubKey("shell\\open\\command");
            shellKey.SetValue(string.Empty, $"\"{exePath}\" \"%1\"");
            shellKey.Dispose();
            key.Dispose();
        }, ct);
    }

    /// <summary>
    /// <para>异步将指定目录添加到系统 PATH 环境变量。</para>
    /// Asynchronously adds the specified directory to the system PATH environment variable.
    /// </summary>
    public Task AddToSystemPathAsync(string directory, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            var envKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Environment", writable: true);
            if (envKey is null) return;

            var pathValue = (string?)envKey.GetValue("Path", string.Empty);
            var paths = (pathValue ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries);
            if (paths.Contains(directory, StringComparer.OrdinalIgnoreCase))
            {
                envKey.Close();
                return;
            }

            var newPath = string.IsNullOrEmpty(pathValue) ? directory : $"{pathValue};{directory}";
            envKey.SetValue("Path", newPath);
            envKey.Close();

            NotifyEnvironmentChange();
        }, ct);
    }

    /// <summary>
    /// <para>异步从系统 PATH 环境变量中移除指定目录。</para>
    /// Asynchronously removes the specified directory from the system PATH environment variable.
    /// </summary>
    public Task RemoveFromSystemPathAsync(string directory, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            var envKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Environment", writable: true);
            if (envKey is null) return;

            var pathValue = (string?)envKey.GetValue("Path", null);
            if (string.IsNullOrEmpty(pathValue))
            {
                envKey.Close();
                return;
            }

            var paths = pathValue.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Where(p => !string.Equals(p, directory, StringComparison.OrdinalIgnoreCase));
            envKey.SetValue("Path", string.Join(";", paths));
            envKey.Close();

            NotifyEnvironmentChange();
        }, ct);
    }

    /// <summary>
    /// <para>通过 SendMessageTimeout 广播 WM_SETTINGCHANGE 消息，通知系统环境变量已更新。</para>
    /// Broadcasts WM_SETTINGCHANGE via SendMessageTimeout to notify the system that environment variables have been updated.
    /// </summary>
    private static void NotifyEnvironmentChange()
    {
        SendMessageTimeout(
            (IntPtr)0xFFFF,
            0x001A,
            IntPtr.Zero,
            "Environment",
            0x0002,
            5000,
            out _);
    }

    /// <summary>
    /// <para>获取指定应用程序的数据目录路径。</para>
    /// Gets the data directory path for the specified application.
    /// </summary>
    public string GetAppDataDirectory(string appName)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, appName);
    }

    /// <summary>
    /// <para>异步获取指定安装路径中应用程序的已安装版本号。</para>
    /// Asynchronously gets the installed version number of the application at the specified installation path.
    /// </summary>
    public Task<string?> GetInstalledVersionAsync(string installPath)
    {
        return Task.Run(() =>
        {
            if (!Directory.Exists(installPath)) return null;

            try
            {
                var exeFiles = Directory.GetFiles(installPath, "*.exe", SearchOption.TopDirectoryOnly);
                string? targetExe = null;
                foreach (var f in exeFiles)
                {
                    var name = Path.GetFileNameWithoutExtension(f);
                    if (name.Length > 0)
                    {
                        targetExe = f;
                        break;
                    }
                }

                if (targetExe is null) return null;

                var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(targetExe);
                return string.IsNullOrEmpty(versionInfo.ProductVersion) ? null : versionInfo.ProductVersion;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get installed version: {ex.Message}");
                return null;
            }
        });
    }

    private static void ShellExecute(string programPath, string? args, string verb)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = programPath,
                Arguments = args ?? string.Empty,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(programPath) ?? string.Empty,
            };
            System.Diagnostics.Process.Start(psi);
            return;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ShellExecute fallback: UseShellExecute=true failed for '{programPath}': {ex.Message}");
        }

        var sei = new SHELLEXECUTEINFOW
        {
            cbSize = (uint)Marshal.SizeOf<SHELLEXECUTEINFOW>(),
            fMask = SEE_MASK_NOCLOSEPROCESS,
            lpVerb = verb,
            lpFile = programPath,
            lpParameters = args ?? string.Empty,
            lpDirectory = Path.GetDirectoryName(programPath) ?? string.Empty,
            nShow = SW_SHOWNORMAL,
        };

        if (!ShellExecuteExW(ref sei))
        {
            var shellError = Marshal.GetLastWin32Error();
            System.Diagnostics.Debug.WriteLine($"ShellExecute fallback: ShellExecuteExW failed for '{programPath}' with Win32 error {shellError}");

            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = programPath,
                    Arguments = args ?? string.Empty,
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(programPath) ?? string.Empty,
                };
                System.Diagnostics.Process.Start(psi);
                return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShellExecute fallback: UseShellExecute=false failed for '{programPath}': {ex.Message}");
                throw new Win32Exception(shellError, ex.Message);
            }
        }

        if (sei.hProcess != IntPtr.Zero)
        {
            CloseHandle(sei.hProcess);
        }
    }

    #region P/Invoke

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHELLEXECUTEINFOW
    {
        public uint cbSize;
        public uint fMask;
        public IntPtr hwnd;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? lpVerb;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? lpFile;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? lpParameters;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? lpDirectory;
        public int nShow;
        public IntPtr hInstApp;
        public IntPtr lpIDList;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? lpClass;
        public IntPtr hkeyClass;
        public uint dwHotKey;
        public IntPtr hIcon;
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

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd, uint Msg, IntPtr wParam, string lParam,
        uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

    #endregion
}
