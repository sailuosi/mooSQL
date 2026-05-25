using System;
using System.Collections.Generic;

namespace mooSQL.data.cluster
{
    /// <summary>
    /// 同步双写 fan-out 执行器。
    /// </summary>
    public static class WriteFanoutExecutor
    {
        public static int ExecuteNonQuery(
            SQLCmd cmd,
            IList<DBInstance> targets,
            DBExecutor executor,
            DualWriteErrorPolicy policy = DualWriteErrorPolicy.MasterWins)
        {
            if (targets == null || targets.Count == 0) return 0;
            EnsureHomogeneousDialects(targets);
            Exception firstSlaveError = null;
            var masterRows = 0;
            for (var i = 0; i < targets.Count; i++)
            {
                var db = targets[i];
                try
                {
                    var perTargetCmd = CloneCmd(cmd);
                    var rows = db.ExeNonQuery(perTargetCmd, executor);
                    if (i == 0) masterRows = rows;
                }
                catch (Exception ex)
                {
                    if (i == 0) throw;
                    firstSlaveError = ex;
                    if (policy == DualWriteErrorPolicy.AllMustSucceed) throw;
                }
            }
            if (firstSlaveError != null && policy == DualWriteErrorPolicy.AllMustSucceed)
                throw firstSlaveError;
            return masterRows;
        }

        private static void EnsureHomogeneousDialects(IList<DBInstance> targets)
        {
            if (targets.Count <= 1) return;
            var first = targets[0]?.config?.dbType;
            for (var i = 1; i < targets.Count; i++)
            {
                if (targets[i]?.config?.dbType != first)
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
