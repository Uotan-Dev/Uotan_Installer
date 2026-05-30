using System.Net.Http.Headers;

namespace UotanInstaller.App.Services;

/// <summary>
/// <para>多线程下载服务实现，提供单线程和多线程下载及测速功能</para>
/// Multi-threaded download service implementation that provides single/multi-threaded download and speed test functionality
/// </summary>
public sealed class DownloadService : IDownloadService
{
    private readonly IHttpService _httpService;

    /// <summary>
    /// <para>初始化DownloadService实例</para>
    /// Initialize the DownloadService instance
    /// </summary>
    /// <param name="httpService">
    /// <para>HTTP服务实例</para>
    /// The HTTP service instance
    /// </param>
    public DownloadService(IHttpService httpService)
    {
        _httpService = httpService;
    }

    /// <inheritdoc/>
    public async Task DownloadFileAsync(string url, string targetPath, IProgress<(long Downloaded, long Total)>? progress = null, CancellationToken cancellationToken = default)
    {
        var client = _httpService.Client;
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;

        var directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        var buffer = new byte[81920];
        long totalRead = 0;
        int bytesRead;
        var lastReportTime = DateTime.UtcNow;

        while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
            totalRead += bytesRead;

            var now = DateTime.UtcNow;
            if ((now - lastReportTime).TotalMilliseconds >= 100)
            {
                lastReportTime = now;
                progress?.Report((totalRead, totalBytes));
            }
        }

        await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        progress?.Report((totalRead, totalBytes > 0 ? totalBytes : totalRead));
    }

    /// <inheritdoc/>
    public async Task MultiThreadedDownloadAsync(string url, string targetPath, int threadCount, IProgress<(long Downloaded, long Total)>? progress = null, CancellationToken cancellationToken = default)
    {
        threadCount = Math.Clamp(threadCount, 2, 8);

        var client = _httpService.Client;

        var supportsRange = await CheckRangeSupportAsync(client, url, cancellationToken).ConfigureAwait(false);
        if (!supportsRange)
        {
            await DownloadFileAsync(url, targetPath, progress, cancellationToken).ConfigureAwait(false);
            return;
        }

        var totalSize = await GetContentLengthAsync(client, url, cancellationToken).ConfigureAwait(false);
        if (totalSize <= 0)
        {
            await DownloadFileAsync(url, targetPath, progress, cancellationToken).ConfigureAwait(false);
            return;
        }

        var directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
        fileStream.SetLength(totalSize);

        var chunkSize = totalSize / threadCount;
        var totalDownloaded = 0L;
        var lockObj = new object();
        var lastReportTime = DateTime.UtcNow;

        var tasks = new Task[threadCount];
        for (var i = 0; i < threadCount; i++)
        {
            var start = i * chunkSize;
            var end = i == threadCount - 1 ? totalSize - 1 : (i + 1) * chunkSize - 1;
            var chunkIndex = i;

            tasks[i] = DownloadChunkAsync(client, url, fileStream, start, end, chunkIndex, bytesRead =>
            {
                lock (lockObj)
                {
                    Interlocked.Add(ref totalDownloaded, bytesRead);
                    var now = DateTime.UtcNow;
                    if ((now - lastReportTime).TotalMilliseconds >= 100)
                    {
                        lastReportTime = now;
                        progress?.Report((Interlocked.Read(ref totalDownloaded), totalSize));
                    }
                }
            }, cancellationToken);
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
        progress?.Report((Interlocked.Read(ref totalDownloaded), totalSize));
    }

    /// <inheritdoc/>
    public async Task<double> SpeedTestAsync(string url, CancellationToken cancellationToken = default)
    {
        var client = _httpService.Client;
        var totalDownloaded = 0L;
        var startTime = DateTime.UtcNow;
        var endTime = DateTime.UtcNow;

        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(linkedCts.Token).ConfigureAwait(false);
            var buffer = new byte[32768];
            int bytesRead;
            var readTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var readLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(linkedCts.Token, readTimeoutCts.Token);

            startTime = DateTime.UtcNow;

            while ((bytesRead = await stream.ReadAsync(buffer, readLinkedCts.Token).ConfigureAwait(false)) > 0)
            {
                Interlocked.Add(ref totalDownloaded, bytesRead);
            }

            endTime = DateTime.UtcNow;
        }
        catch (OperationCanceledException)
        {
            endTime = DateTime.UtcNow;
        }

        var totalBytes = Interlocked.Read(ref totalDownloaded);
        if (totalBytes == 0) return -1;

        var elapsed = (endTime - startTime).TotalSeconds;
        if (elapsed <= 0) return -1;

        return totalBytes / elapsed / 1024.0 / 1024.0;
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, double>> BatchSpeedTestAsync(IEnumerable<string> urls, CancellationToken cancellationToken = default)
    {
        var urlList = urls.ToList();
        var results = new Dictionary<string, double>();

        using var overallTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, overallTimeoutCts.Token);

        var tasks = urlList.Select(async url =>
        {
            try
            {
                var speed = await SpeedTestAsync(url, linkedCts.Token).ConfigureAwait(false);
                return (url, speed);
            }
            catch
            {
                return (url, -1.0);
            }
        });

        var completed = await Task.WhenAll(tasks).ConfigureAwait(false);

        foreach (var (url, speed) in completed)
        {
            results[url] = speed;
        }

        return results;
    }

    private static async Task DownloadChunkAsync(
        HttpClient client,
        string url,
        FileStream fileStream,
        long start,
        long end,
        int chunkIndex,
        Action<long> onProgress,
        CancellationToken cancellationToken)
    {
        const int maxRetries = 3;
        var retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Range = new RangeHeaderValue(start, end);

                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                var buffer = new byte[32768];
                var currentPos = start;
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    lock (fileStream)
                    {
                        fileStream.Position = currentPos;
                        fileStream.Write(buffer, 0, bytesRead);
                    }

                    currentPos += bytesRead;
                    onProgress(bytesRead);
                }

                return;
            }
            catch (Exception) when (retryCount < maxRetries - 1)
            {
                retryCount++;
                await Task.Delay(500 * retryCount, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static async Task<bool> CheckRangeSupportAsync(HttpClient client, string url, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            return response.Headers.AcceptRanges.Contains("bytes");
        }
        catch
        {
            return false;
        }
    }

    private static async Task<long> GetContentLengthAsync(HttpClient client, string url, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return response.Content.Headers.ContentLength ?? 0;
        }
        catch
        {
            return 0;
        }
    }
}
