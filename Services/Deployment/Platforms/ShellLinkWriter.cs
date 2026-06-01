using System.Runtime.Versioning;
using System.Text;

namespace UotanInstaller.App.Services.Deployment.Platforms;

/// <summary>
/// <para>按照 MS-SHLLINK 二进制文件格式规范直接写入 Windows 快捷方式（.lnk）文件，无需 COM 互操作，完全兼容 AOT 发布。</para>
/// Writes Windows shortcut (.lnk) files directly according to the MS-SHLLINK binary file format specification,
/// without COM interop, fully compatible with AOT publishing.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class ShellLinkWriter
{
    private static readonly Guid LinkClsid = new("00021401-0000-0000-C000-000000000046");

    private const uint HasWorkingDir = 0x00000010;
    private const uint HasArguments = 0x00000020;
    private const uint IsUnicode = 0x00000080;
    private const uint HasExpString = 0x00000200;
    private const uint PreferEnvironmentPath = 0x02000000;

    /// <summary>
    /// <para>创建 Windows 快捷方式（.lnk）文件。使用 EnvironmentVariableDataBlock 存储目标路径，无需 LinkTargetIDList 和 LinkInfo，确保在目标文件不存在时仍可正确创建快捷方式。</para>
    /// Creates a Windows shortcut (.lnk) file. Uses the EnvironmentVariableDataBlock to store the target path,
    /// without requiring LinkTargetIDList or LinkInfo, ensuring the shortcut can be created correctly even when the target file does not exist.
    /// </summary>
    /// <param name="shortcutPath">
    /// <para>快捷方式文件的完整路径（.lnk 文件路径）。</para>
    /// The full path of the shortcut file (.lnk file path).
    /// </param>
    /// <param name="targetPath">
    /// <para>快捷方式指向的目标可执行文件路径。</para>
    /// The target executable file path that the shortcut points to.
    /// </param>
    /// <param name="arguments">
    /// <para>启动目标程序时传递的命令行参数，可为 null。</para>
    /// The command-line arguments passed when launching the target program, or null.
    /// </param>
    /// <param name="workingDirectory">
    /// <para>快捷方式的工作目录路径，可为 null。</para>
    /// The working directory path for the shortcut, or null.
    /// </param>
    /// <param name="showCommand">
    /// <para>窗口显示方式：1=正常，3=最大化，7=最小化。</para>
    /// The window show command: 1=Normal, 3=Maximized, 7=Minimized.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <para>当目标路径超过最大允许长度时抛出。</para>
    /// Thrown when the target path exceeds the maximum allowed length.
    /// </exception>
    public static void CreateShortcut(
        string shortcutPath,
        string targetPath,
        string? arguments = null,
        string? workingDirectory = null,
        int showCommand = 1)
    {
        if (targetPath.Length >= 260)
            throw new ArgumentException(
                $"Target path exceeds maximum length of 259 characters (actual: {targetPath.Length}).");

        uint linkFlags = IsUnicode | HasExpString | PreferEnvironmentPath;

        if (!string.IsNullOrEmpty(workingDirectory))
            linkFlags |= HasWorkingDir;

        if (!string.IsNullOrEmpty(arguments))
            linkFlags |= HasArguments;

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        WriteHeader(writer, linkFlags, showCommand);
        WriteStringData(writer, workingDirectory, arguments);
        WriteEnvironmentVariableDataBlock(writer, targetPath);
        writer.Write(0u);

        File.WriteAllBytes(shortcutPath, stream.ToArray());
    }

    /// <summary>
    /// <para>写入 ShellLinkHeader 结构（76 字节），包含快捷方式的标识信息、时间戳和标志位。</para>
    /// Writes the ShellLinkHeader structure (76 bytes), containing identification information, timestamps, and flags for the shortcut.
    /// </summary>
    private static void WriteHeader(BinaryWriter writer, uint linkFlags, int showCommand)
    {
        writer.Write(0x4Cu);
        writer.Write(LinkClsid.ToByteArray());
        writer.Write(linkFlags);
        writer.Write(0u);
        writer.Write(0L);
        writer.Write(0L);
        writer.Write(0L);
        writer.Write(0u);
        writer.Write(0);
        writer.Write((uint)showCommand);
        writer.Write((ushort)0);
        writer.Write((ushort)0);
        writer.Write(0u);
        writer.Write(0u);
    }

    /// <summary>
    /// <para>写入 StringData 结构，按 MS-SHLLINK 规范顺序依次写入 WORKING_DIR 和 COMMAND_LINE_ARGUMENTS 字符串。</para>
    /// Writes the StringData structure, writing WORKING_DIR and COMMAND_LINE_ARGUMENTS strings in the order specified by the MS-SHLLINK specification.
    /// </summary>
    private static void WriteStringData(BinaryWriter writer, string? workingDirectory, string? arguments)
    {
        if (!string.IsNullOrEmpty(workingDirectory))
            WriteUnicodePrefixedString(writer, workingDirectory);

        if (!string.IsNullOrEmpty(arguments))
            WriteUnicodePrefixedString(writer, arguments);
    }

    /// <summary>
    /// <para>写入一个 Unicode 前缀长度字符串：2 字节字符计数（含 null 终止符）+ UTF-16LE 编码字符串数据 + 2 字节 null 终止符。</para>
    /// Writes a Unicode prefixed-length string: 2-byte character count (including null terminator) + UTF-16LE encoded string data + 2-byte null terminator.
    /// </summary>
    private static void WriteUnicodePrefixedString(BinaryWriter writer, string value)
    {
        ushort charCount = (ushort)(value.Length + 1);
        writer.Write(charCount);
        writer.Write(Encoding.Unicode.GetBytes(value));
        writer.Write((ushort)0);
    }

    /// <summary>
    /// <para>写入 EnvironmentVariableDataBlock（788 字节），包含 ANSI 和 Unicode 两种编码的目标路径。Windows 使用此数据块在缺少 LinkTargetIDList 时解析快捷方式目标。</para>
    /// Writes the EnvironmentVariableDataBlock (788 bytes), containing the target path in both ANSI and Unicode encodings.
    /// Windows uses this data block to resolve the shortcut target when LinkTargetIDList is absent.
    /// </summary>
    private static void WriteEnvironmentVariableDataBlock(BinaryWriter writer, string targetPath)
    {
        writer.Write(0x314u);
        writer.Write(0xA0000001u);
        WriteFixedAnsiString(writer, targetPath, 260);
        WriteFixedUnicodeString(writer, targetPath, 520);
    }

    /// <summary>
    /// <para>写入固定长度的 ANSI 字符串，不足部分用零填充。</para>
    /// Writes a fixed-length ANSI string, padded with zeros if shorter than the specified total bytes.
    /// </summary>
    private static void WriteFixedAnsiString(BinaryWriter writer, string value, int totalBytes)
    {
        var buffer = new byte[totalBytes];
        var bytes = Encoding.Default.GetBytes(value);
        var copyLen = Math.Min(bytes.Length, totalBytes - 1);
        Buffer.BlockCopy(bytes, 0, buffer, 0, copyLen);
        writer.Write(buffer);
    }

    /// <summary>
    /// <para>写入固定长度的 Unicode 字符串，不足部分用零填充。</para>
    /// Writes a fixed-length Unicode string, padded with zeros if shorter than the specified total bytes.
    /// </summary>
    private static void WriteFixedUnicodeString(BinaryWriter writer, string value, int totalBytes)
    {
        var buffer = new byte[totalBytes];
        var bytes = Encoding.Unicode.GetBytes(value);
        var copyLen = Math.Min(bytes.Length, totalBytes - 2);
        Buffer.BlockCopy(bytes, 0, buffer, 0, copyLen);
        writer.Write(buffer);
    }
}
