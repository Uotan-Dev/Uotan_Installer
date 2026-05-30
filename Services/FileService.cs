using System.Security.Cryptography;

namespace UotanInstaller.App.Services;

/// <summary>
/// <para>文件服务实现，提供哈希计算和临时文件清理功能</para>
/// File service implementation that provides hash computation and temporary file cleanup functionality
/// </summary>
public sealed class FileService : IFileService
{
    /// <inheritdoc/>
    public async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken = default)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <inheritdoc/>
    public async Task CleanupTempFilesAsync(string directory, string filePattern, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            if (!Directory.Exists(directory)) return;

            try
            {
                var files = Directory.GetFiles(directory, filePattern);
                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try { File.Delete(file); } catch { }
                }
            }
            catch { }
        }, cancellationToken).ConfigureAwait(false);
    }
}
