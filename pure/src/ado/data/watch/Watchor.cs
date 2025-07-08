
using mooSQL.data.context;
using System;


namespace mooSQL.data
{
    public  class Watchor:IWatchor
    {
 
        public override string onBeforeExecuteSet(ExeContext context, string operation) {
            return Guid.NewGuid().ToString();
        }


        public override string onAfterExecuteSet(string oprationId, ExeContext context, string operation) {
            return "";
        }

        public override string onAfterExecuteSetError(string oprationId, ExeContext context, Exception ex, string operation) {
            return "";
        }

        public override string WriteDbSessionOpenBefore(ExeSession session) {
            return Guid.NewGuid().ToString();
        }

        public override string WriteDbSessionOpenAfter(string oprationId, ExeSession session) {
            return "";
        }

        public override string WriteDbSessionOpenError(string oprationId, ExeSession session, Exception ex) {
            OnSessionError(oprationId, session, ex);
            return "";
        }

        public override string WriteDbSessionBeginTransactionBefore(ExeSession exeSession)
        {
            return Guid.NewGuid().ToString();
        }

        public override void WriteDbSessionBeginTransactionAfter(string operationId, ExeSession exeSession)
        {

        }

        public override void WriteDbSessionBeginTransactionError(string operationId, ExeSession exeSession, Exception ex)
        {
            OnSessionError(operationId, exeSession, ex);
        }

        public override string WriteDbSessionCommitBefore(ExeSession exeSession)
        {
            return Guid.NewGuid().ToString();
        }

        public override void WriteDbSessionCommitAfter(string operationId, ExeSession exeSession)
        {

        }

        public override void WriteDbSessionCommitError(string operationId, ExeSession exeSession, Exception ex)
        {
            OnSessionError(operationId, exeSession, ex);
        }

        public override string WriteDbSessionRollbackBefore(ExeSession exeSession)
        {
            return Guid.NewGuid().ToString();
        }

        public override void WriteDbSessionRollbackAfter(string operationId, ExeSession exeSession)
        {

        }

        public override void WriteDbSessionRollbackError(string operationId, ExeSession exeSession, Exception ex)
        {
            OnSessionError(operationId, exeSession, ex);
        }

        public override string WriteDbSessionDisposeBefore(ExeSession exeSession)
        {
            return Guid.NewGuid().ToString();
        }

        public override void WriteDbSessionDisposeAfter(string operationId, ExeSession exeSession)
        {

        }

        public override void WriteDbSessionDisposeError(string operationId, ExeSession exeSession, Exception ex)
        {
            OnSessionError(operationId, exeSession, ex);
        }

        public virtual void OnSessionError(string operationId, ExeSession exeSession, Exception ex) { 
            
        }
    }
}