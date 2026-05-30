namespace UotanInstaller.App.Services;

/// <summary>
/// <para>多线程下载服务接口，提供单线程和多线程下载及测速功能</para>
/// Multi-threaded download service interface that provides single/multi-threaded download and speed test functionality
/// </summary>
public interface IDownloadService
{
    /// <summary>
    /// <para>单线程下载文件</para>
    /// Download a file using a single thread
    /// </summary>
    /// <param name="url">
    /// <para>下载的URL</para>
    /// The download URL
    /// </param>
    /// <param name="targetPath">
    /// <para>目标文件路径</para>
    /// The target file path
    /// </param>
    /// <param name="progress">
    /// <para>进度回调，包含已下载字节数和总字节数</para>
    /// Progress callback with downloaded bytes and total bytes
    /// </param>
    /// <param name="cancellationToken">
    /// <para>取消令牌</para>
    /// Cancellation token
    /// </param>
    /// <returns>
    /// <para>异步任务</para>
    /// An asynchronous task
    /// </returns>
    Task DownloadFileAsync(string url, string targetPath, IProgress<(long Downloaded, long Total)>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>多线程下载文件</para>
    /// Download a file using multiple threads
    /// </summary>
    /// <param name="url">
    /// <para>下载的URL</para>
    /// The download URL
    /// </param>
    /// <param name="targetPath">
    /// <para>目标文件路径</para>
    /// The target file path
    /// </param>
    /// <param name="threadCount">
    /// <para>下载线程数（2-8之间）</para>
    /// The number of download threads (between 2 and 8)
    /// </param>
    /// <param name="progress">
    /// <para>进度回调，包含已下载字节数和总字节数</para>
    /// Progress callback with downloaded bytes and total bytes
    /// </param>
    /// <param name="cancellationToken">
    /// <para>取消令牌</para>
    /// Cancellation token
    /// </param>
    /// <returns>
    /// <para>异步任务</para>
    /// An asynchronous task
    /// </returns>
    Task MultiThreadedDownloadAsync(string url, string targetPath, int threadCount, IProgress<(long Downloaded, long Total)>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>执行5MB测速</para>
    /// Perform a 5MB speed test
    /// </summary>
    /// <param name="url">
    /// <para>测速用的URL</para>
    /// The URL for speed testing
    /// </param>
    /// <param name="cancellationToken">
    /// <para>取消令牌</para>
    /// Cancellation token
    /// </param>
    /// <returns>
    /// <para>下载速度（MB/s），失败返回-1</para>
    /// Download speed in MB/s, or -1 on failure
    /// </returns>
    Task<double> SpeedTestAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>对多个 URL 执行并发测速</para>
    /// Perform concurrent speed tests on multiple URLs
    /// </summary>
    /// <param name="urls">
    /// <para>需要测速的 URL 列表</para>
    /// The list of URLs to speed test
    /// </param>
    /// <param name="cancellationToken">
    /// <para>取消令牌</para>
    /// Cancellation token
    /// </param>
    /// <returns>
    /// <para>URL 到测速结果（MB/s）的字典，失败时值为 -1</para>
    /// A dictionary mapping URL to speed test result in MB/s, or -1 on failure
    /// </returns>
    Task<Dictionary<string, double>> BatchSpeedTestAsync(IEnumerable<string> urls, CancellationToken cancellationToken = default);
}
