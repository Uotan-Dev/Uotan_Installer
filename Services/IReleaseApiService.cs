using UotanInstaller.App.Models;

namespace UotanInstaller.App.Services;

/// <summary>
/// <para>发布API服务接口，提供与发布服务器交互的所有API</para>
/// Release API service interface that provides all APIs for interacting with the release server
/// </summary>
public interface IReleaseApiService
{
    /// <summary>
    /// <para>获取补丁数据</para>
    /// Get patch data
    /// </summary>
    /// <param name="cancellationToken">
    /// <para>取消令牌</para>
    /// Cancellation token
    /// </param>
    /// <returns>
    /// <para>补丁数据</para>
    /// The patch data
    /// </returns>
    Task<GenericPatchData> GetPatchDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>检查安装器自更新</para>
    /// Check for installer self-update
    /// </summary>
    /// <param name="currentVersion">
    /// <para>当前版本号</para>
    /// The current version number
    /// </param>
    /// <param name="cancellationToken">
    /// <para>取消令牌</para>
    /// Cancellation token
    /// </param>
    /// <returns>
    /// <para>如果有新版本返回true，否则返回false</para>
    /// True if a newer version is available; otherwise false
    /// </returns>
    Task<bool> CheckSelfUpdateAsync(string currentVersion, CancellationToken cancellationToken = default);
}
