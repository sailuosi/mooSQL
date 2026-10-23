using System;
using mooSQL.data.model;

namespace mooSQL.data
{
    public partial class SQLCmd
    {
        /// <summary>
        /// 按 <see cref="type"/> 选举执行路径。
        /// <see cref="QueryType.InsertReturnId"/> → Scalar 取主键；其它类型请用 Builder 的 exeNonQuery/query，避免与纯粹 NonQuery 混淆。
        /// </summary>
        /// <param name="db">数据库实例（复用其会话/连接）。</param>
        /// <param name="executor">可选事务执行器；与 insert 同连接硬约束。</param>
        public object run(DBInstance db, DBExecutor executor = null)
        {
            if (db == null)
                throw new ArgumentNullException(nameof(db));
            EnsureLiveParasResolved();
            if (Empty)
                throw new InvalidOperationException("SQLCmd.run: sql is empty.");

            if (type == QueryType.InsertReturnId)
                return db.ExeQueryScalar(this, executor);

            throw new InvalidOperationException(
                "SQLCmd.run currently supports QueryType.InsertReturnId only. " +
                "Use exeNonQuery / query APIs for other command types (executor stays pure).");
        }

        /// <summary>
        /// <see cref="run(DBInstance, DBExecutor)"/> 的强类型封装。
        /// </summary>
        public TKey runKey<TKey>(DBInstance db, DBExecutor executor = null)
        {
            var id = run(db, executor);
            if (id == null || id is DBNull)
                return default(TKey);
            if (id is TKey direct)
                return direct;
            return (TKey)Convert.ChangeType(id, typeof(TKey));
        }
    }
}
