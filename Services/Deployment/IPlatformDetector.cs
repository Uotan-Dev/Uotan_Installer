using System.Runtime.InteropServices;

namespace UotanInstaller.App.Services.Deployment;

/// <summary>
/// <para>表示操作系统平台类型。</para>
/// Represents the operating system platform type.
/// </summary>
public enum PlatformOS
{
    /// <summary>
    /// <para>Windows 操作系统。</para>
    /// Windows operating system.
    /// </summary>
    Windows,

    /// <summary>
    /// <para>macOS 操作系统。</para>
    /// macOS operating system.
    /// </summary>
    macOS,

    /// <summary>
    /// <para>Linux 操作系统。</para>
    /// Linux operating system.
    /// </summary>
    Linux,

    /// <summary>
    /// <para>未知操作系统。</para>
    /// Unknown operating system.
    /// </summary>
    Unknown,
}

/// <summary>
/// <para>封装当前平台检测的结果信息。</para>
/// Encapsulates the result information of the current platform detection.
/// </summary>
public sealed class PlatformInfo
{
    /// <summary>
    /// <para>获取或初始化检测到的操作系统平台。</para>
    /// Gets or initializes the detected operating system platform.
    /// </summary>
    public PlatformOS OS { get; init; }

    /// <summary>
    /// <para>获取或初始化系统处理器架构。</para>
    /// Gets or initializes the system processor architecture.
    /// </summary>
    public Architecture Architecture { get; init; }

    /// <summary>
    /// <para>获取或初始化当前平台是否受支持。</para>
    /// Gets or initializes whether the current platform is supported.
    /// </summary>
    public bool IsSupported { get; init; }

    /// <summary>
    /// <para>获取或初始化操作系统版本字符串。</para>
    /// Gets or initializes the operating system version string.
    /// </summary>
    public string Version { get; init; } = string.Empty;
}

/// <summary>
/// <para>提供平台检测能力，用于识别当前运行环境的操作系统、架构及版本信息。</para>
/// Provides platform detection capability to identify the operating system, architecture, and version of the current runtime environment.
/// </summary>
public interface IPlatformDetector
{
    /// <summary>
    /// <para>检测当前运行平台的详细信息。</para>
    /// Detects detailed information about the current runtime platform.
    /// </summary>
    /// <returns>
    /// <para>包含操作系统、架构、版本及支持状态的平台信息。</para>
    /// Platform information containing the OS, architecture, version, and support status.
    /// </returns>
    PlatformInfo Detect();
}
