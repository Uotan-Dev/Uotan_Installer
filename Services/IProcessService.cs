namespace UotanInstaller.App.Services;

/// <summary>
/// <para>进程管理服务接口，提供进程查询和终止功能</para>
/// Process management service interface that provides process query and termination functionality
/// </summary>
public interface IProcessService
{
    /// <summary>
    /// <para>检查指定名称的进程是否正在运行</para>
    /// Check if a process with the specified name is running
    /// </summary>
    /// <param name="processName">
    /// <para>进程名称（如 "UotanToolbox.exe"）</para>
    /// The process name (e.g., "UotanToolbox.exe")
    /// </param>
    /// <param name="pathPart">
    /// <para>可选的路径部分过滤条件</para>
    /// Optional path part filter condition
    /// </param>
    /// <returns>
    /// <para>包含是否运行和进程ID的元组</para>
    /// A tuple containing whether the process is running and the process ID
    /// </returns>
    (bool IsRunning, int? Pid) IsProcessRunning(string processName, string? pathPart = null);

    /// <summary>
    /// <para>终止指定进程ID的进程</para>
    /// Terminate the process with the specified process ID
    /// </summary>
    /// <param name="pid">
    /// <para>要终止的进程ID</para>
    /// The process ID to terminate
    /// </param>
    void KillProcess(int pid);

    /// <summary>
    /// <para>等待指定进程退出</para>
    /// Wait for the specified process to exit
    /// </summary>
    /// <param name="pid">
    /// <para>要等待的进程ID</para>
    /// The process ID to wait for
    /// </param>
    /// <returns>
    /// <para>进程的退出代码</para>
    /// The exit code of the process
    /// </returns>
    int WaitForProcessExit(int pid);
}
