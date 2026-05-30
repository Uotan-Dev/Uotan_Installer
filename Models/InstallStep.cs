namespace UotanInstaller.App.Models;

/// <summary>
/// <para>表示安装程序的步骤枚举。</para>
/// Represents the installation step enumeration.
/// </summary>
public enum InstallStep
{
    /// <summary>
    /// <para>最终用户许可协议步骤。</para>
    /// End User License Agreement step.
    /// </summary>
    Eula = 1,

    /// <summary>
    /// <para>选择镜像源步骤。</para>
    /// Choose mirror step.
    /// </summary>
    ChooseMirror = 3,

    /// <summary>
    /// <para>正在安装步骤。</para>
    /// Installing step.
    /// </summary>
    Installing = 4,

    /// <summary>
    /// <para>安装完成步骤。</para>
    /// Finish step.
    /// </summary>
    Finish = 5,

    /// <summary>
    /// <para>已安装步骤。</para>
    /// Already installed step.
    /// </summary>
    AlreadyInstalled = 6,
}
