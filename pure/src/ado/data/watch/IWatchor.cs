
using mooSQL.data.context;
using System;

namespace mooSQL.data
{
    /// <summary>
    /// SQL 执行监视器抽象基类，提供执行前后钩子。
    /// </summary>
    public abstract partial class IWatchor {

        /// <summary>
        /// 抽象方法 onBeforeExecuteSet（返回 string），由子类实现。
        /// </summary>
        public abstract string onBeforeExecuteSet(ExeContext context,string operation);


        /// <summary>
        /// 抽象方法 onAfterExecuteSet（返回 string），由子类实现。
        /// </summary>
        public abstract string onAfterExecuteSet(string oprationId, ExeContext context, string operation);

        /// <summary>
        /// 抽象方法 onAfterExecuteSetError（返回 string），由子类实现。
        /// </summary>
        public abstract string onAfterExecuteSetError(string oprationId, ExeContext context,Exception ex, string operation);

        /// <summary>
        /// 抽象方法 WriteDbSessionOpenBefore（返回 string），由子类实现。
        /// </summary>
        public abstract string WriteDbSessionOpenBefore(ExeSession session);

        /// <summary>
        /// 抽象方法 WriteDbSessionOpenAfter（返回 string），由子类实现。
        /// </summary>
        public abstract string WriteDbSessionOpenAfter(string oprationId,ExeSession session);

        /// <summary>
        /// 抽象方法 WriteDbSessionOpenError（返回 string），由子类实现。
        /// </summary>
        public abstract string WriteDbSessionOpenError(string oprationId, ExeSession session,Exception ex);
        /// <summary>
        /// 抽象方法 WriteDbSessionBeginTransactionBefore（返回 string），由子类实现。
        /// </summary>
        public abstract string WriteDbSessionBeginTransactionBefore(ExeSession exeSession);
        /// <summary>
        /// 抽象方法 WriteDbSessionBeginTransactionAfter（返回 void），由子类实现。
        /// </summary>
        public abstract void WriteDbSessionBeginTransactionAfter(string operationId, ExeSession exeSession);
        /// <summary>
        /// 抽象方法 WriteDbSessionBeginTransactionError（返回 void），由子类实现。
        /// </summary>
        public abstract void WriteDbSessionBeginTransactionError(string operationId, ExeSession exeSession, Exception ex);
        /// <summary>
        /// 抽象方法 WriteDbSessionCommitBefore（返回 string），由子类实现。
        /// </summary>
        public abstract string WriteDbSessionCommitBefore(ExeSession exeSession);
        /// <summary>
        /// 抽象方法 WriteDbSessionCommitAfter（返回 void），由子类实现。
        /// </summary>
        public abstract void WriteDbSessionCommitAfter(string operationId, ExeSession exeSession);
        /// <summary>
        /// 抽象方法 WriteDbSessionCommitError（返回 void），由子类实现。
        /// </summary>
        public abstract void WriteDbSessionCommitError(string operationId, ExeSession exeSession, Exception ex);
        /// <summary>
        /// 抽象方法 WriteDbSessionRollbackBefore（返回 string），由子类实现。
        /// </summary>
        public abstract string WriteDbSessionRollbackBefore(ExeSession exeSession);
        /// <summary>
        /// 抽象方法 WriteDbSessionRollbackAfter（返回 void），由子类实现。
        /// </summary>
        public abstract void WriteDbSessionRollbackAfter(string operationId, ExeSession exeSession);
        /// <summary>
        /// 抽象方法 WriteDbSessionRollbackError（返回 void），由子类实现。
        /// </summary>
        public abstract void WriteDbSessionRollbackError(string operationId, ExeSession exeSession, Exception ex);
        /// <summary>
        /// 抽象方法 WriteDbSessionDisposeBefore（返回 string），由子类实现。
        /// </summary>
        public abstract string WriteDbSessionDisposeBefore(ExeSession exeSession);
        /// <summary>
        /// 抽象方法 WriteDbSessionDisposeAfter（返回 void），由子类实现。
        /// </summary>
        public abstract void WriteDbSessionDisposeAfter(string operationId, ExeSession exeSession);
        /// <summary>
        /// 抽象方法 WriteDbSessionDisposeError（返回 void），由子类实现。
        /// </summary>
        public abstract void WriteDbSessionDisposeError(string operationId, ExeSession exeSession, Exception ex);
    }
}