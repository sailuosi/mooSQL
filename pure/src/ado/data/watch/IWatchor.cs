
using mooSQL.data.context;
using System;

namespace mooSQL.data
{
    public abstract partial class IWatchor {

        public abstract string onBeforeExecuteSet(ExeContext context,string operation);


        public abstract string onAfterExecuteSet(string oprationId, ExeContext context, string operation);

        public abstract string onAfterExecuteSetError(string oprationId, ExeContext context,Exception ex, string operation);

        public abstract string WriteDbSessionOpenBefore(ExeSession session);

        public abstract string WriteDbSessionOpenAfter(string oprationId,ExeSession session);

        public abstract string WriteDbSessionOpenError(string oprationId, ExeSession session,Exception ex);
        public abstract string WriteDbSessionBeginTransactionBefore(ExeSession exeSession);
        public abstract void WriteDbSessionBeginTransactionAfter(string operationId, ExeSession exeSession);
        public abstract void WriteDbSessionBeginTransactionError(string operationId, ExeSession exeSession, Exception ex);
        public abstract string WriteDbSessionCommitBefore(ExeSession exeSession);
        public abstract void WriteDbSessionCommitAfter(string operationId, ExeSession exeSession);
        public abstract void WriteDbSessionCommitError(string operationId, ExeSession exeSession, Exception ex);
        public abstract string WriteDbSessionRollbackBefore(ExeSession exeSession);
        public abstract void WriteDbSessionRollbackAfter(string operationId, ExeSession exeSession);
        public abstract void WriteDbSessionRollbackError(string operationId, ExeSession exeSession, Exception ex);
        public abstract string WriteDbSessionDisposeBefore(ExeSession exeSession);
        public abstract void WriteDbSessionDisposeAfter(string operationId, ExeSession exeSession);
        public abstract void WriteDbSessionDisposeError(string operationId, ExeSession exeSession, Exception ex);
    }
}