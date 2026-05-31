using System.Diagnostics;
using System.Runtime.Versioning;

namespace UotanInstaller.App.Services.Deployment.Platforms;

/// <summary>
/// <para>macOS 平台适配器实现，提供安装目录解析、快捷方式创建、应用启动及权限提升等 macOS 特定操作。</para>
/// macOS platform adapter implementation, providing macOS-specific operations such as install directory resolution, shortcut creation, application launching, and privilege elevation.
/// </summary>
[SupportedOSPlatform("macos")]
public sealed class MacOSPlatformAdapter : IPlatformAdapter
{
    /// <summary>
    /// <para>获取指定应用程序在 macOS 上的默认安装目录路径（/Applications 目录下）。</para>
    /// Gets the default installation directory path for the specified application under /Applications on macOS.
    /// </summary>
    /// <param name="appName">
    /// <para>应用程序名称。</para>
    /// The application name.
    /// </param>
    /// <returns>
    /// <para>默认安装目录的绝对路径，格式为 /Applications/{appName}.app。</para>
    /// The absolute path of the default installation directory, in the format /Applications/{appName}.app.
    /// </returns>
    public string GetDefaultInstallDirectory(string appName)
    {
        return $"/Applications/{appName}.app";
    }

    /// <summary>
    /// <para>在 macOS 桌面创建应用程序快捷方式（Finder 别名）。</para>
    /// Creates an application shortcut (Finder alias) on the macOS desktop.
    /// </summary>
    /// <param name="appName">
    /// <para>应用程序名称，用作快捷方式名称。</para>
    /// The application name, used as the shortcut name.
    /// </param>
    /// <param name="targetPath">
    /// <para>快捷方式指向的目标 .app 包路径。</para>
    /// The target .app bundle path that the shortcut points to.
    /// </param>
    /// <param name="args">
    /// <para>启动目标程序时传递的命令行参数，可为 null（macOS 快捷方式不支持参数）。</para>
    /// The command-line arguments passed when launching the target program, or null (macOS shortcuts do not support arguments).
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

            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var desktopDir = Path.Combine(homeDir, "Desktop");
            var aliasPath = Path.Combine(desktopDir, $"{appName}");

            if (!Directory.Exists(desktopDir))
            {
                Directory.CreateDirectory(desktopDir);
            }

            if (File.Exists(aliasPath) || Directory.Exists(aliasPath))
            {
                try
                {
                    if (Directory.Exists(aliasPath))
                        Directory.Delete(aliasPath, recursive: true);
                    else
                        File.Delete(aliasPath);
                }
                catch
                {
                }
            }

            var escapedTarget = targetPath.Replace("\\", "\\\\").Replace("\"", "\\\"");
            var escapedAlias = aliasPath.Replace("\\", "\\\\").Replace("\"", "\\\"");
            var script = $"tell application \"Finder\" to make alias file to (POSIX file \"{escapedTarget}\") at (POSIX file \"{Path.GetDirectoryName(aliasPath)}/\")";

            var psi = new ProcessStartInfo
            {
                FileName = "osascript",
                Arguments = $"-e '{script.Replace("'", "'\\''")}'",
                UseShellExecute = false,
                RedirectStandardError = true,
            };

            using var process = Process.Start(psi)
                ?? throw new DeploymentException($"Failed to create alias from '{targetPath}' to '{aliasPath}'.");
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();
                throw new DeploymentException($"Failed to create alias: {error}");
            }
        }, ct);
    }

    /// <summary>
    /// <para>使用 macOS 的 open 命令启动指定的应用程序。</para>
    /// Launches the specified application using the macOS open command.
    /// </summary>
    /// <param name="exePath">
    /// <para>可执行文件或 .app 包的路径。</para>
    /// The executable file or .app bundle path.
    /// </param>
    /// <param name="args">
    /// <para>启动时传递的命令行参数，可为 null。</para>
    /// The command-line arguments passed at launch, or null.
    /// </param>
    public void LaunchApplication(string exePath, string? args = null)
    {
        var arguments = string.IsNullOrEmpty(args) ? $"\"{exePath}\"" : $"\"{exePath}\" --args {args}";

        var psi = new ProcessStartInfo
        {
            FileName = "open",
            Arguments = arguments,
            UseShellExecute = false,
        };

        Process.Start(psi)?.Dispose();
    }

    /// <summary>
    /// <para>使用 osascript 以管理员权限启动指定程序，将弹出 macOS 授权对话框。</para>
    /// Launches the specified program with administrator privileges using osascript, which will display a macOS authorization dialog.
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
        var escapedPath = exePath.Replace("\\", "\\\\").Replace("\"", "\\\"");
        var scriptArgs = string.IsNullOrEmpty(args)
            ? $"\"do shell script \\\"\\\\\\\"{escapedPath}\\\\\\\"\\\" with administrator privileges\""
            : $"\"do shell script \\\"\\\\\\\"{escapedPath}\\\\\\\" {args.Replace("\"", "\\\\\\\"")}\\\" with administrator privileges\"";

        var psi = new ProcessStartInfo
        {
            FileName = "osascript",
            Arguments = $"-e {scriptArgs}",
            UseShellExecute = false,
        };

        Process.Start(psi)?.Dispose();
    }

    /// <summary>
    /// <para>获取 macOS 平台的临时文件目录路径。</para>
    /// Gets the temporary file directory path on macOS.
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

            var macosDir = Path.Combine(installPath, "Contents", "MacOS");
            if (Directory.Exists(macosDir))
            {
                var files = Directory.GetFiles(macosDir);
                if (files.Length > 0) return files[0];
            }

            try
            {
                foreach (var file in Directory.GetFiles(installPath))
                {
                    if (!Path.GetFileName(file).Contains('.'))
                    {
                        return file;
                    }
                }
            }
            catch
            {
            }

            return null;
        });
    }

    public Task OpenInFileExplorerAsync(string path)
    {
        return Task.Run(() =>
        {
            var psi = new ProcessStartInfo
            {
                FileName = "open",
                Arguments = $"\"{path}\"",
                UseShellExecute = false,
            };
            Process.Start(psi)?.Dispose();
        });
    }
}
