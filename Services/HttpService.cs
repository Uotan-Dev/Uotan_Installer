using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace UotanInstaller.App.Services;

/// <summary>
/// <para>HTTP客户端服务实现，封装HttpClient操作</para>
/// HTTP client service implementation that wraps HttpClient operations
/// </summary>
public sealed class HttpService : IHttpService, IDisposable
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// <para>初始化HttpService实例</para>
    /// Initialize the HttpService instance
    /// </summary>
    public HttpService()
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(10),
        };

        _httpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("UotanToolboxInstaller", "1.0.0"));
    }

    /// <inheritdoc/>
    public HttpClient Client => _httpClient;

    /// <inheritdoc/>
    public async Task<T> GetAsync<T>(string url, JsonTypeInfo<T>? jsonTypeInfo = null, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        var typeInfo = jsonTypeInfo ?? AppJsonContext.Default.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>
            ?? throw new InvalidOperationException($"No JsonTypeInfo registered for type {typeof(T).Name}.");

        return JsonSerializer.Deserialize(json, typeInfo)
            ?? throw new InvalidOperationException("Failed to deserialize response.");
    }

    /// <summary>
    /// <para>释放HTTP客户端资源</para>
    /// Dispose the HTTP client resources
    /// </summary>
    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
