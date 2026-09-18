using System;
using System.Collections.Generic;

namespace mooSQL.data.cluster
{
    /// <summary>
    /// 同步双写 fan-out 执行器：主库单独写 + 从库列表 fan-out。
    /// </summary>
    public static class WriteFanoutExecutor
    {
        /// <summary>
        /// 先写主库（复用调用方 Executor / 事务），再向从库独立连接 fan-out。
        /// </summary>
        /// <param name="cmd">待执行 SQL</param>
        /// <param name="master">主库实例</param>
        /// <param name="slaves">双写从库列表（不含主库）</param>
        /// <param name="executor">主库执行器（保留事务）</param>
        /// <param name="policy">从库失败策略</param>
        public static int ExecuteNonQuery(
            SQLCmd cmd,
            DBInstance master,
            IList<DBInstance> slaves,
            DBExecutor executor,
            DualWriteErrorPolicy policy = DualWriteErrorPolicy.MasterWins)
        {
            if (master == null)
                throw new ArgumentNullException(nameof(master));

            EnsureHomogeneousDialects(master, slaves);

            var masterRows = master.ExeNonQuery(cmd, executor);

            if (slaves == null || slaves.Count == 0)
                return masterRows;

            Exception firstSlaveError = null;
            for (var i = 0; i < slaves.Count; i++)
            {
                var db = slaves[i];
                if (db == null) continue;
                try
                {
                    // 从库必须独立 Executor，否则会忽略目标实例、打到主库连接
                    var sres= db.ExeNonQuery(cmd);
                }
                catch (Exception ex)
                {
                    firstSlaveError = ex;
                    if (policy == DualWriteErrorPolicy.AllMustSucceed) throw;
                }
            }
            if (firstSlaveError != null && policy == DualWriteErrorPolicy.AllMustSucceed)
                throw firstSlaveError;
            return masterRows;
        }

        private static void EnsureHomogeneousDialects(DBInstance master, IList<DBInstance> slaves)
        {
            var first = master?.config?.dbType;
            if (slaves == null) return;
            for (var i = 0; i < slaves.Count; i++)
            {
                if (slaves[i]?.config?.dbType != first)
                    throw new InvalidOperationException(
                        "DualWrite 要求组内数据库类型一致；跨方言双写需业务侧按目标库分别编制 SQL。");
            }
        }

        private static SQLCmd CloneCmd(SQLCmd source)
        {
            if (source == null) return null;
            var clone = new SQLCmd(source.sql, source.para)
            {
                type = source.type,
                timeout = source.timeout,
                cmdType = source.cmdType,
                signal = source.signal,
                TargetTable = source.TargetTable
            };
            return clone;
        }
    }
}
