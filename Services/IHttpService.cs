using System.Text.Json.Serialization.Metadata;

namespace UotanInstaller.App.Services;

/// <summary>
/// <para>HTTP客户端服务接口，封装HttpClient操作</para>
/// HTTP client service interface that wraps HttpClient operations
/// </summary>
public interface IHttpService : IDisposable
{
    /// <summary>
    /// <para>获取内部HttpClient实例</para>
    /// Get the internal HttpClient instance
    /// </summary>
    HttpClient Client { get; }

    /// <summary>
    /// <para>发送GET请求并反序列化JSON响应</para>
    /// Send a GET request and deserialize the JSON response
    /// </summary>
    Task<T> GetAsync<T>(string url, JsonTypeInfo<T>? jsonTypeInfo = null, CancellationToken cancellationToken = default);
}
