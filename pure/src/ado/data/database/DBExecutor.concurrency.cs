using System.Threading;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public partial class DBExecutor
    {
        /// <summary>
        /// 同一执行器上的 ExecuteCmd 族调用串行化，避免共享连接被并发 Open/Dispose 还池污染其它请求。
        /// 灾切重试等请调用不带门禁的 Core 方法，勿在已占用时再次 Enter。
        /// </summary>
        private readonly SemaphoreSlim _executionLock = new SemaphoreSlim(1, 1);

        private void EnterExecutionGate()
        {
            _executionLock.Wait();
        }

        private Task EnterExecutionGateAsync()
        {
            return _executionLock.WaitAsync();
        }

        private void ExitExecutionGate()
        {
            _executionLock.Release();
        }

        /// <summary>
        /// 非 KeepOpen 时释放会话并丢弃 Context，避免下一轮复用已关闭的 session/connection。
        /// </summary>
        private void ReleaseSessionAfterExecute()
        {
            if (KeepOpen)
                return;
            if (Context?.session != null)
                Context.session.Dispose();
            Context = null!;
        }
    }
}
