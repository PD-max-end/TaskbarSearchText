using System.Diagnostics;

namespace TaskbarSearchText;

/// <summary>
/// 负责以先关闭、后强制结束的顺序重启 Windows 资源管理器。
/// </summary>
internal static class ExplorerHelper
{
    private const int GracefulCloseTimeoutMilliseconds = 800;
    private const int ForcedExitTimeoutMilliseconds = 2000;

    /// <summary>
    /// 关闭所有 explorer.exe 实例，并在最后重新启动资源管理器。
    /// </summary>
    public static void Restart()
    {
        foreach (Process process in Process.GetProcessesByName("explorer"))
        {
            try
            {
                // 优先请求窗口正常关闭。
                process.CloseMainWindow();
            }
            catch (Exception)
            {
                // 进程可能已经退出，继续执行后续确认逻辑。
            }

            try
            {
                // 等待正常关闭，超过 800ms 后强制结束。
                if (!process.WaitForExit(GracefulCloseTimeoutMilliseconds))
                {
                    process.Kill();
                    process.WaitForExit(ForcedExitTimeoutMilliseconds);
                }
            }
            catch (Exception)
            {
                // 极短时间内的进程状态变化不应中断整个重启流程。
            }
            finally
            {
                process.Dispose();
            }
        }

        // 最后启动 explorer.exe，确保资源管理器重新运行。
        Process.Start(new ProcessStartInfo("explorer.exe")
        {
            UseShellExecute = true
        });
    }
}
