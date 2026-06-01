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
        var programFiles = Environment.GetEnvironmentVariable("ProgramW6432")
            ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        return Path.Combine(programFiles, appName);
    }

    /// <summary>
    /// <para>在 Windows 桌面创建应用程序快捷方式（.lnk 文件），直接写入 Shell Link 二进制格式，无需 COM 互操作。</para>
    /// Creates an application shortcut (.lnk file) on the Windows desktop by writing the Shell Link binary format directly, without COM interop.
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

            WriteShortcutBinary(lnkPath, targetPath, args ?? string.Empty, Path.GetDirectoryName(targetPath) ?? string.Empty);
        }, ct);
    }

    /// <summary>
    /// <para>直接写入 Shell Link (.lnk) 二进制文件格式，使用 ILCreateFromPathW P/Invoke 获取 PIDL，完全避免 COM 互操作。</para>
    /// Writes the Shell Link (.lnk) binary file format directly, using ILCreateFromPathW P/Invoke to obtain the PIDL, completely avoiding COM interop.
    /// </summary>
    /// <param name="shortcutPath">
    /// <para>快捷方式文件的完整路径。</para>
    /// The full path of the shortcut file.
    /// </param>
    /// <param name="targetPath">
    /// <para>目标可执行文件路径。</para>
    /// The target executable file path.
    /// </param>
    /// <param name="arguments">
    /// <para>命令行参数。</para>
    /// The command-line arguments.
    /// </param>
    /// <param name="workingDirectory">
    /// <para>工作目录。</para>
    /// The working directory.
    /// </param>
    private static void WriteShortcutBinary(string shortcutPath, string targetPath, string arguments, string workingDirectory)
    {
        var pidl = ILCreateFromPathW(targetPath);

        if (pidl == IntPtr.Zero)
        {
            WriteShortcutWithExpString(shortcutPath, targetPath, arguments, workingDirectory);
            return;
        }

        try
        {
            uint linkFlags = LinkFlags.HasLinkTargetIDList
                           | LinkFlags.IsUnicode
                           | LinkFlags.HasWorkingDir;

            if (!string.IsNullOrEmpty(arguments))
                linkFlags |= LinkFlags.HasArguments;

            using var stream = new FileStream(shortcutPath, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(stream);

            WriteShellLinkHeader(writer, linkFlags);
            WriteLinkTargetIdList(writer, pidl);

            WriteUnicodeStringData(writer, workingDirectory);
            if (!string.IsNullOrEmpty(arguments))
                WriteUnicodeStringData(writer, arguments);

            writer.Write(0u);
        }
        finally
        {
            ILFree(pidl);
        }
    }

    /// <summary>
    /// <para>当 ILCreateFromPathW 失败时，使用 EnvironmentVariableDataBlock 写入 .lnk 文件作为降级方案。</para>
    /// When ILCreateFromPathW fails, writes a .lnk file using the EnvironmentVariableDataBlock as a fallback.
    /// </summary>
    private static void WriteShortcutWithExpString(string shortcutPath, string targetPath, string arguments, string workingDirectory)
    {
        uint linkFlags = LinkFlags.IsUnicode
                       | LinkFlags.HasWorkingDir
                       | LinkFlags.HasExpString;

        if (!string.IsNullOrEmpty(arguments))
            linkFlags |= LinkFlags.HasArguments;

        using var stream = new FileStream(shortcutPath, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream);

        WriteShellLinkHeader(writer, linkFlags);

        WriteUnicodeStringData(writer, workingDirectory);
        if (!string.IsNullOrEmpty(arguments))
            WriteUnicodeStringData(writer, arguments);

        WriteEnvironmentVariableBlock(writer, targetPath);

        writer.Write(0u);
    }

    /// <summary>
    /// <para>写入 ShellLinkHeader 结构（76 字节）。</para>
    /// Writes the ShellLinkHeader structure (76 bytes).
    /// </summary>
    private static void WriteShellLinkHeader(BinaryWriter writer, uint linkFlags)
    {
        writer.Write(0x4Cu);
        writer.Write(ShellLinkCLSID.ToByteArrayLe());
        writer.Write(linkFlags);
        writer.Write(0x20u);
        writer.Write(0L);
        writer.Write(0L);
        writer.Write(0L);
        writer.Write(0u);
        writer.Write(0);
        writer.Write(1);
        writer.Write((ushort)0);
        writer.Write((ushort)0);
        writer.Write(0u);
        writer.Write(0u);
    }

    /// <summary>
    /// <para>写入 LinkTargetIDList 结构，包含从 ILCreateFromPathW 获取的 PIDL。</para>
    /// Writes the LinkTargetIDList structure containing the PIDL obtained from ILCreateFromPathW.
    /// </summary>
    private static void WriteLinkTargetIdList(BinaryWriter writer, IntPtr pidl)
    {
        int idListDataSize = 0;
        var current = pidl;
        while (true)
        {
            var cb = Marshal.ReadInt16(current);
            if (cb == 0) break;
            idListDataSize += cb;
            current = IntPtr.Add(current, cb);
        }

        var idListSize = (ushort)(idListDataSize + 2);
        writer.Write(idListSize);

        current = pidl;
        while (true)
        {
            var cb = Marshal.ReadInt16(current);
            if (cb == 0) break;
            var bytes = new byte[cb];
            Marshal.Copy(current, bytes, 0, cb);
            writer.Write(bytes);
            current = IntPtr.Add(current, cb);
        }

        writer.Write((ushort)0);
    }

    /// <summary>
    /// <para>写入 Unicode 字符串数据段（字符计数 + UTF-16LE 编码字符串 + 空终止符）。</para>
    /// Writes a Unicode string data section (character count + UTF-16LE encoded string + null terminator).
    /// </summary>
    private static void WriteUnicodeStringData(BinaryWriter writer, string value)
    {
        var charCount = (ushort)(value.Length + 1);
        writer.Write(charCount);
        writer.Write(Encoding.Unicode.GetBytes(value));
        writer.Write((ushort)0);
    }

    /// <summary>
    /// <para>写入 EnvironmentVariableDataBlock（签名 0xA0000001），包含目标路径的 ANSI 和 Unicode 表示。</para>
    /// Writes the EnvironmentVariableDataBlock (signature 0xA0000001) containing the target path in both ANSI and Unicode representations.
    /// </summary>
    private static void WriteEnvironmentVariableBlock(BinaryWriter writer, string targetPath)
    {
        var ansiBuffer = new byte[260];
        var unicodeBuffer = new byte[520];

        var ansiBytes = Encoding.Default.GetBytes(targetPath);
        Array.Copy(ansiBytes, 0, ansiBuffer, 0, Math.Min(ansiBytes.Length, 259));

        var unicodeBytes = Encoding.Unicode.GetBytes(targetPath);
        Array.Copy(unicodeBytes, 0, unicodeBuffer, 0, Math.Min(unicodeBytes.Length, 518));

        uint blockSize = 4 + 4 + 260 + 520;
        writer.Write(blockSize);
        writer.Write(0xA0000001u);
        writer.Write(ansiBuffer);
        writer.Write(unicodeBuffer);
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

    private static readonly Guid ShellLinkCLSID = new("00021401-0000-0000-C000-000000000046");

    private static class LinkFlags
    {
        public const uint HasLinkTargetIDList = 0x00000001;
        public const uint HasWorkingDir = 0x00000010;
        public const uint HasArguments = 0x00000020;
        public const uint IsUnicode = 0x00000080;
        public const uint HasExpString = 0x00000200;
    }

    #region P/Invoke

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr ILCreateFromPathW(string pszPath);

    [DllImport("shell32.dll")]
    private static extern void ILFree(IntPtr pidl);

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

    #endregion
}

file static class GuidExtensions
{
    public static byte[] ToByteArrayLe(this Guid guid)
    {
        var bytes = guid.ToByteArray();
        return bytes;
    }
}
