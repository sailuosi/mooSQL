using mooSQL.data.context;

using System.Data;
using System.Data.Common;


namespace mooSQL.data
{
    /// <summary>
    /// 接口 ITransaction。
    /// </summary>
    public interface ITransaction
    {
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        DbTransaction BeginTransaction(ExeContext context);
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        DbTransaction BeginTransaction(IsolationLevel isolationLevel, ExeContext context);
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        void CommitTransaction();
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        void RollbackTransaction();
    }
}