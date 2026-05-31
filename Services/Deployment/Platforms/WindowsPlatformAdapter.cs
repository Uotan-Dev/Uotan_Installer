using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

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
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        return Path.Combine(programFiles, appName);
    }

    /// <summary>
    /// <para>在 Windows 桌面创建应用程序快捷方式（.lnk 文件）。</para>
    /// Creates an application shortcut (.lnk file) on the Windows desktop.
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

            if (!Directory.Exists(desktopPath))
            {
                Directory.CreateDirectory(desktopPath);
            }

            var shellLink = (IShellLinkW)new ShellLink();
            shellLink.SetPath(targetPath);

            if (!string.IsNullOrEmpty(args))
            {
                shellLink.SetArguments(args);
            }

            shellLink.SetWorkingDirectory(Path.GetDirectoryName(targetPath) ?? string.Empty);

            var persistFile = (IPersistFile)shellLink;
            persistFile.Save(lnkPath, false);

            Marshal.ReleaseComObject(persistFile);
            Marshal.ReleaseComObject(shellLink);
        }, ct);
    }

    /// <summary>
    /// <para>使用 ShellExecuteExW 以 "open" 动词启动指定的应用程序。</para>
    /// Launches the specified application using ShellExecuteExW with the "open" verb.
    /// </summary>
    /// <param name="exePath">
    /// <para>可执行文件的路径。</para>
    /// The executable file path.
    /// </param>
    /// <param name="args">
    /// <para>启动时传递的命令行参数，可为 null。</para>
    /// The command-line arguments passed at launch, or null.
    /// </param>
    public void LaunchApplication(string exePath, string? args = null)
    {
        ShellExecute(exePath, args, "open");
    }

    /// <summary>
    /// <para>使用 ShellExecuteExW 以 "runas" 动词请求管理员权限启动指定程序。</para>
    /// Launches the specified program with administrator privileges using ShellExecuteExW with the "runas" verb.
    /// </summary>
    /// <param name="exePath">
    /// <para>可执行文件的路径。</para>
    /// The executable file path.
    /// </param>
    /// <param name="args">
    /// <para>启动时传递的命令行参数，可为 null。</para>
    /// The command-line arguments passed at launch, or null.
    /// </param>
    public void RequestElevation(string exePath, string? args = null)
    {
        ShellExecute(exePath, args, "runas");
    }

    /// <summary>
    /// <para>获取 Windows 平台的临时文件目录路径。</para>
    /// Gets the temporary file directory path on Windows.
    /// </summary>
    /// <returns>
    /// <para>临时文件目录的绝对路径。</para>
    /// The absolute path of the temporary file directory.
    /// </returns>
    public string GetTempDirectory() => Path.GetTempPath();

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

    private static void ShellExecute(string programPath, string? args, string verb)
    {
        var sei = new SHELLEXECUTEINFOW
        {
            cbSize = (uint)Marshal.SizeOf<SHELLEXECUTEINFOW>(),
            fMask = SEE_MASK_NOCLOSEPROCESS,
            lpVerb = verb,
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

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    [CoClass(typeof(ShellLinkClass))]
    private interface ShellLink : IShellLinkW;

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLinkClass;

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, uint fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(IntPtr hwnd, uint fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("0000010b-0000-0000-C000-000000000046")]
    private interface IPersistFile
    {
        void GetClassID(out Guid pClassID);
        void IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }
}
