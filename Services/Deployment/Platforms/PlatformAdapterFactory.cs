namespace UotanInstaller.App.Services.Deployment.Platforms;

/// <summary>
/// <para>平台适配器工厂，根据平台检测结果创建对应的平台适配器实例。</para>
/// Platform adapter factory that creates the corresponding platform adapter instance based on the platform detection result.
/// </summary>
public static class PlatformAdapterFactory
{
    /// <summary>
    /// <para>根据指定的平台检测器检测结果，创建并返回适用于当前平台的适配器实例。</para>
    /// Creates and returns a platform adapter instance suitable for the current platform based on the specified platform detector's detection result.
    /// </summary>
    /// <param name="detector">
    /// <para>用于检测当前平台的平台检测器实例。</para>
    /// The platform detector instance used to detect the current platform.
    /// </param>
    /// <returns>
    /// <para>与当前平台匹配的 IPlatformAdapter 实例。</para>
    /// An IPlatformAdapter instance matching the current platform.
    /// </returns>
    /// <exception cref="PlatformNotSupportedDeploymentException">
    /// <para>当前平台不受支持（PlatformOS.Unknown）时抛出。</para>
    /// Thrown when the current platform is not supported (PlatformOS.Unknown).
    /// </exception>
    public static IPlatformAdapter Create(IPlatformDetector detector)
    {
        var info = detector.Detect();

#pragma warning disable CA1416
        return info.OS switch
        {
            PlatformOS.Windows => new WindowsPlatformAdapter(),
            PlatformOS.macOS => new MacOSPlatformAdapter(),
            PlatformOS.Linux => new LinuxPlatformAdapter(),
            _ => throw new PlatformNotSupportedDeploymentException(
                $"The current platform '{info.OS}' is not supported.",
                info.OS),
        };
#pragma warning restore CA1416
    }
}
