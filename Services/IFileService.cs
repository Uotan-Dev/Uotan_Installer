namespace UotanInstaller.App.Services;

/// <summary>
/// <para>文件服务接口，提供哈希计算和临时文件清理功能</para>
/// File service interface that provides hash computation and temporary file cleanup functionality
/// </summary>
public interface IFileService
{
    /// <summary>
    /// <para>异步计算文件的SHA256哈希值</para>
    /// Asynchronously compute the SHA256 hash of a file
    /// </summary>
    /// <param name="filePath">
    /// <para>文件路径</para>
    /// The file path
    /// </param>
    /// <param name="cancellationToken">
    /// <para>取消令牌</para>
    /// Cancellation token
    /// </param>
    /// <returns>
    /// <para>小写的十六进制SHA256哈希字符串</para>
    /// The lowercase hexadecimal SHA256 hash string
    /// </returns>
    Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>清理指定目录中的临时文件</para>
    /// Clean up temporary files in the specified directory
    /// </summary>
    /// <param name="directory">
    /// <para>要清理的目录路径</para>
    /// The directory path to clean up
    /// </param>
    /// <param name="filePattern">
    /// <para>要清理的文件匹配模式，如 "*.zip"</para>
    /// The file pattern to clean up, e.g. "*.zip"
    /// </param>
    /// <param name="cancellationToken">
    /// <para>取消令牌</para>
    /// Cancellation token
    /// </param>
    /// <returns>
    /// <para>异步任务</para>
    /// An asynchronous task
    /// </returns>
    Task CleanupTempFilesAsync(string directory, string filePattern, CancellationToken cancellationToken = default);
}
