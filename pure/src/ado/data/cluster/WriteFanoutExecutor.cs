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
            Exception firstSlaveError = null;
            var masterRows = 0;
            for (var i = 0; i < targets.Count; i++)
            {
                var db = targets[i];
                try
                {
                    var rows = db.ExeNonQuery(cmd, executor);
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
    }
}
