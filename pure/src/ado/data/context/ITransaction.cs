using mooSQL.data.context;

using System.Data;
using System.Data.Common;


namespace mooSQL.data
{
    public interface ITransaction
    {
        DbTransaction BeginTransaction(ExeContext context);
        DbTransaction BeginTransaction(IsolationLevel isolationLevel, ExeContext context);
        void CommitTransaction();
        void RollbackTransaction();
    }
}
