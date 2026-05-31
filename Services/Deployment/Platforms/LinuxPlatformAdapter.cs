using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;

namespace UotanInstaller.App.Services.Deployment.Platforms;

/// <summary>
/// <para>Linux 平台适配器实现，提供安装目录解析、桌面快捷方式创建、应用启动及权限提升等 Linux 特定操作。</para>
/// Linux platform adapter implementation, providing Linux-specific operations such as install directory resolution, desktop shortcut creation, application launching, and privilege elevation.
/// </summary>
[SupportedOSPlatform("linux")]
public sealed class LinuxPlatformAdapter : IPlatformAdapter
{
    /// <summary>
    /// <para>获取指定应用程序在 Linux 上的默认安装目录路径（/opt 目录下）。</para>
    /// Gets the default installation directory path for the specified application under /opt on Linux.
    /// </summary>
    /// <param name="appName">
    /// <para>应用程序名称。</para>
    /// The application name.
    /// </param>
    /// <returns>
    /// <para>默认安装目录的绝对路径，格式为 /opt/{appName}。</para>
    /// The absolute path of the default installation directory, in the format /opt/{appName}.
    /// </returns>
    public string GetDefaultInstallDirectory(string appName)
    {
        return $"/opt/{appName}";
    }

    /// <summary>
    /// <para>在 Linux 桌面创建应用程序快捷方式（.desktop 文件），遵循 FreeDesktop.org 桌面入口规范。</para>
    /// Creates an application shortcut (.desktop file) on the Linux desktop, following the FreeDesktop.org Desktop Entry Specification.
    /// </summary>
    /// <param name="appName">
    /// <para>应用程序名称，用作快捷方式显示名称。</para>
    /// The application name, used as the shortcut display name.
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

            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var desktopDir = Path.Combine(homeDir, "Desktop");
            var desktopFilePath = Path.Combine(desktopDir, $"{appName}.desktop");

            if (!Directory.Exists(desktopDir))
            {
                Directory.CreateDirectory(desktopDir);
            }

            var execLine = string.IsNullOrEmpty(args)
                ? $"Exec={targetPath}"
                : $"Exec={targetPath} {args}";

            var content = new StringBuilder();
            content.AppendLine("[Desktop Entry]");
            content.AppendLine("Type=Application");
            content.AppendLine($"Name={appName}");
            content.AppendLine(execLine);
            content.AppendLine($"Icon={appName}");
            content.AppendLine("Terminal=false");
            content.AppendLine("Categories=Utility;");

            File.WriteAllText(desktopFilePath, content.ToString());

            var chmodPsi = new ProcessStartInfo
            {
                FileName = "chmod",
                Arguments = $"+x \"{desktopFilePath}\"",
                UseShellExecute = false,
            };

            using var chmodProcess = Process.Start(chmodPsi)
                ?? throw new DeploymentException($"Failed to set executable permission on '{desktopFilePath}'.");
            chmodProcess.WaitForExit();

            var applicationsDir = Path.Combine(homeDir, ".local", "share", "applications");
            if (!Directory.Exists(applicationsDir))
            {
                Directory.CreateDirectory(applicationsDir);
            }

            var appDesktopFilePath = Path.Combine(applicationsDir, $"{appName}.desktop");
            try
            {
                File.WriteAllText(appDesktopFilePath, content.ToString());
                var chmodAppPsi = new ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"+x \"{appDesktopFilePath}\"",
                    UseShellExecute = false,
                };
                using var chmodAppProcess = Process.Start(chmodAppPsi);
                chmodAppProcess?.WaitForExit();
            }
            catch
            {
            }

            try
            {
                var updateDbPsi = new ProcessStartInfo
                {
                    FileName = "update-desktop-database",
                    Arguments = applicationsDir,
                    UseShellExecute = false,
                };
                using var updateDbProcess = Process.Start(updateDbPsi);
                updateDbProcess?.WaitForExit();
            }
            catch
            {
            }
        }, ct);
    }

    /// <summary>
    /// <para>启动指定的应用程序，先确保可执行权限后直接启动。</para>
    /// Launches the specified application, ensuring execute permission before direct launch.
    /// </summary>
    /// <param name="exePath">
    /// <para>可执行文件的路径。</para>
    /// The executable file path.
    /// </param>
    /// <param name="args">
    /// <para>启动时传递的命令行参数，可为 null。</para>
    /// The command-line arguments passed at launch, or null.
    /// </param>
    /// <exception cref="DeploymentException">
    /// <para>当指定的可执行文件不存在时抛出。</para>
    /// Thrown when the specified executable file does not exist.
    /// </exception>
    public void LaunchApplication(string exePath, string? args = null)
    {
        if (!File.Exists(exePath))
            throw new DeploymentException($"Executable not found: {exePath}");

        try
        {
            var chmodPsi = new ProcessStartInfo
            {
                FileName = "chmod",
                Arguments = $"+x \"{exePath}\"",
                UseShellExecute = false,
            };
            using var chmodProcess = Process.Start(chmodPsi);
            chmodProcess?.WaitForExit();
        }
        catch
        {
        }

        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = args ?? string.Empty,
            UseShellExecute = false,
        };
        Process.Start(psi)?.Dispose();
    }

    /// <summary>
    /// <para>使用 pkexec 以管理员权限启动指定程序，将弹出 Linux 授权对话框。若 pkexec 不可用则抛出异常。</para>
    /// Launches the specified program with administrator privileges using pkexec, which will display a Linux authorization dialog. Throws if pkexec is not available.
    /// </summary>
    /// <param name="exePath">
    /// <para>可执行文件的路径。</para>
    /// The executable file path.
    /// </param>
    /// <param name="args">
    /// <para>启动时传递的命令行参数，可为 null。</para>
    /// The command-line arguments passed at launch, or null.
    /// </param>
    /// <exception cref="PlatformNotSupportedDeploymentException">
    /// <para>当系统中未安装 pkexec 时抛出。</para>
    /// Thrown when pkexec is not installed on the system.
    /// </exception>
    public void RequestElevation(string exePath, string? args = null)
    {
        var pkexecPath = FindExecutableOnPath("pkexec");
        if (pkexecPath is null)
        {
            throw new PlatformNotSupportedDeploymentException(
                "pkexec is not available. Please install polkit or pkexec to enable privilege elevation.",
                PlatformOS.Linux);
        }

        var arguments = string.IsNullOrEmpty(args) ? $"\"{exePath}\"" : $"\"{exePath}\" {args}";

        var psi = new ProcessStartInfo
        {
            FileName = "pkexec",
            Arguments = arguments,
            UseShellExecute = false,
        };

        Process.Start(psi)?.Dispose();
    }

    /// <summary>
    /// <para>在系统 PATH 中查找指定可执行文件的完整路径。</para>
    /// Searches the system PATH for the full path of the specified executable.
    /// </summary>
    /// <param name="name">
    /// <para>要查找的可执行文件名称。</para>
    /// The name of the executable to find.
    /// </param>
    /// <returns>
    /// <para>可执行文件的完整路径；若未找到则返回 null。</para>
    /// The full path of the executable; or null if not found.
    /// </returns>
    private static string? FindExecutableOnPath(string name)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "which",
                Arguments = name,
                UseShellExecute = false,
                RedirectStandardOutput = true,
            };
            using var process = Process.Start(psi);
            if (process is null) return null;
            process.WaitForExit();
            if (process.ExitCode != 0) return null;
            var result = process.StandardOutput.ReadLine();
            return string.IsNullOrWhiteSpace(result) ? null : result.Trim();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// <para>获取 Linux 平台的临时文件目录路径。</para>
    /// Gets the temporary file directory path on Linux.
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

            try
            {
                foreach (var file in Directory.GetFiles(installPath))
                {
                    var fileName = Path.GetFileName(file);
                    if (fileName.Contains("UotanToolbox", StringComparison.OrdinalIgnoreCase) &&
                        !fileName.Contains('.') &&
                        IsExecutable(file))
                    {
                        return file;
                    }
                }

                foreach (var file in Directory.GetFiles(installPath))
                {
                    if (!Path.GetFileName(file).Contains('.') && IsExecutable(file))
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
                FileName = "xdg-open",
                Arguments = $"\"{path}\"",
                UseShellExecute = false,
            };
            Process.Start(psi)?.Dispose();
        });
    }

    private static bool IsExecutable(string filePath)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "test",
                Arguments = $"-x \"{filePath}\"",
                UseShellExecute = false,
            };
            using var process = Process.Start(psi);
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
