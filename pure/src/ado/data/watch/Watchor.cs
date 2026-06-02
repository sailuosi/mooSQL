
using mooSQL.data.context;
using System;


namespace mooSQL.data
{
    /// <summary>
    /// 类型 Watchor。
    /// </summary>
    public  class Watchor:IWatchor
    {
 
        /// <summary>
        /// onBeforeExecuteSet 方法（返回 string）。
        /// </summary>
        public override string onBeforeExecuteSet(ExeContext context, string operation) {
            return Guid.NewGuid().ToString();
        }


        /// <summary>
        /// onAfterExecuteSet 方法（返回 string）。
        /// </summary>
        public override string onAfterExecuteSet(string oprationId, ExeContext context, string operation) {
            return "";
        }

        /// <summary>
        /// onAfterExecuteSetError 方法（返回 string）。
        /// </summary>
        public override string onAfterExecuteSetError(string oprationId, ExeContext context, Exception ex, string operation) {
            return "";
        }

        /// <summary>
        /// WriteDbSessionOpenBefore 方法（返回 string）。
        /// </summary>
        public override string WriteDbSessionOpenBefore(ExeSession session) {
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// WriteDbSessionOpenAfter 方法（返回 string）。
        /// </summary>
        public override string WriteDbSessionOpenAfter(string oprationId, ExeSession session) {
            return "";
        }

        /// <summary>
        /// WriteDbSessionOpenError 方法（返回 string）。
        /// </summary>
        public override string WriteDbSessionOpenError(string oprationId, ExeSession session, Exception ex) {
            OnSessionError(oprationId, session, ex);
            return "";
        }

        /// <summary>
        /// WriteDbSessionBeginTransactionBefore 方法（返回 string）。
        /// </summary>
        public override string WriteDbSessionBeginTransactionBefore(ExeSession exeSession)
        {
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// WriteDbSessionBeginTransactionAfter 方法。
        /// </summary>
        public override void WriteDbSessionBeginTransactionAfter(string operationId, ExeSession exeSession)
        {

        }

        /// <summary>
        /// WriteDbSessionBeginTransactionError 方法。
        /// </summary>
        public override void WriteDbSessionBeginTransactionError(string operationId, ExeSession exeSession, Exception ex)
        {
            OnSessionError(operationId, exeSession, ex);
        }

        /// <summary>
        /// WriteDbSessionCommitBefore 方法（返回 string）。
        /// </summary>
        public override string WriteDbSessionCommitBefore(ExeSession exeSession)
        {
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// WriteDbSessionCommitAfter 方法。
        /// </summary>
        public override void WriteDbSessionCommitAfter(string operationId, ExeSession exeSession)
        {

        }

        /// <summary>
        /// WriteDbSessionCommitError 方法。
        /// </summary>
        public override void WriteDbSessionCommitError(string operationId, ExeSession exeSession, Exception ex)
        {
            OnSessionError(operationId, exeSession, ex);
        }

        /// <summary>
        /// WriteDbSessionRollbackBefore 方法（返回 string）。
        /// </summary>
        public override string WriteDbSessionRollbackBefore(ExeSession exeSession)
        {
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// WriteDbSessionRollbackAfter 方法。
        /// </summary>
        public override void WriteDbSessionRollbackAfter(string operationId, ExeSession exeSession)
        {

        }

        /// <summary>
        /// WriteDbSessionRollbackError 方法。
        /// </summary>
        public override void WriteDbSessionRollbackError(string operationId, ExeSession exeSession, Exception ex)
        {
            OnSessionError(operationId, exeSession, ex);
        }

        /// <summary>
        /// WriteDbSessionDisposeBefore 方法（返回 string）。
        /// </summary>
        public override string WriteDbSessionDisposeBefore(ExeSession exeSession)
        {
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// WriteDbSessionDisposeAfter 方法。
        /// </summary>
        public override void WriteDbSessionDisposeAfter(string operationId, ExeSession exeSession)
        {

        }

        /// <summary>
        /// WriteDbSessionDisposeError 方法。
        /// </summary>
        public override void WriteDbSessionDisposeError(string operationId, ExeSession exeSession, Exception ex)
        {
            OnSessionError(operationId, exeSession, ex);
        }

        /// <summary>
        /// OnSessionError 方法。
        /// </summary>
        public virtual void OnSessionError(string operationId, ExeSession exeSession, Exception ex) { 
            
        }
    }
}