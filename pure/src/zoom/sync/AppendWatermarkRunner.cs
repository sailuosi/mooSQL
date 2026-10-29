///////////////////////////////////////////////////////////
//  AppendWatermarkRunner.cs
///////////////////////////////////////////////////////////

namespace mooSQL.data.sync
{
    internal static class AppendWatermarkRunner
    {
        public static bool Run<TSrc, TDst>(SyncTableJob<TSrc, TDst> job, SooDataSyncHost host)
            where TSrc : class, new()
            where TDst : class, new()
        {
            var pull = job.ResolvePullMode();
            if (pull == SooDataSyncPullMode.FromSql || pull == SooDataSyncPullMode.EntityByPk)
                return PageAppendRunner.Run(job, host, probePk: false);
            return AppendOnlyRunner.WatermarkAppend(job, host, probePk: false);
        }
    }
}
