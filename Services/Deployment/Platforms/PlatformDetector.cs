using System.Runtime.InteropServices;

namespace UotanInstaller.App.Services.Deployment.Platforms;

/// <summary>
/// <para>提供基于运行时信息的平台检测实现，识别当前操作系统的类型、架构和版本。</para>
/// Provides runtime-information-based platform detection implementation, identifying the current OS type, architecture, and version.
/// </summary>
public sealed class PlatformDetector : IPlatformDetector
{
    /// <summary>
    /// <para>检测当前运行平台的详细信息，包括操作系统类型、处理器架构、版本字符串及支持状态。</para>
    /// Detects detailed information about the current runtime platform, including OS type, processor architecture, version string, and support status.
    /// </summary>
    /// <returns>
    /// <para>包含操作系统、架构、版本及支持状态的平台信息。</para>
    /// Platform information containing the OS, architecture, version, and support status.
    /// </returns>
    public PlatformInfo Detect()
    {
        var os = DetectOS();
        var architecture = RuntimeInformation.OSArchitecture;
        var version = GetOSVersion();
        var isSupported = os != PlatformOS.Unknown;

        return new PlatformInfo
        {
            OS = os,
            Architecture = architecture,
            IsSupported = isSupported,
            Version = version,
        };
    }

    private static PlatformOS DetectOS()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return PlatformOS.Windows;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return PlatformOS.macOS;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return PlatformOS.Linux;
        return PlatformOS.Unknown;
    }

    private static string GetOSVersion()
    {
        var description = RuntimeInformation.OSDescription;
        if (!string.IsNullOrWhiteSpace(description))
            return description;

        try
        {
            return Environment.OSVersion.VersionString;
        }
        catch
        {
            return string.Empty;
        }
    }
}
