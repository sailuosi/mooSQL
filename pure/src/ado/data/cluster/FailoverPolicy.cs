using System;
using System.Linq;
using mooSQL.data.health;

namespace mooSQL.data.cluster
{
    /// <summary>
    /// 组内可写实例选举：不写回组级状态，支持连续选举（排除当前病态实例）。
    /// </summary>
    public static class FailoverPolicy
    {
        /// <summary>
        /// 判断是否为InstanceHealthy。
        /// </summary>
        public static bool IsInstanceHealthy(DBInstance db)
        {
            if (db?.Health == null) return true;
            return db.Health.Status == DBHealthStatus.Available
                   || db.Health.Status == DBHealthStatus.None;
        }

        /// <summary>
        /// 若 <paramref name="current"/> 可用则返回；否则选举下一可用可写实例（配置主或热备）。
        /// </summary>
        public static DBInstance ElectIfUnavailable(
            MasterSlaveGroup group,
            DBInsCash cash,
            MasterSlaveOptions options,
            FailoverContext ctx,
            DBInstance current)
        {
            if (group == null) return current;

            if (current != null && IsInstanceHealthy(current))
                return current;

            if (options?.CustomFailoverElector != null && ctx != null)
            {
                var custom = options.CustomFailoverElector(ctx);
                if (custom != null && IsInstanceHealthy(custom))
                {
                    FillFailoverContext(ctx, current, custom);
                    return custom;
                }
            }

            if (group.Master != null
                && IsInstanceHealthy(group.Master)
                && !ReferenceEquals(group.Master, current))
            {
                FillFailoverContext(ctx, current, group.Master);
                return group.Master;
            }

            var lagSelector = options?.GetReplicationLag;
            foreach (var s in OrderHotStandbyCandidates(group, lagSelector))
            {
                if (!s.CanFailover) continue;
                var inst = s.Instance ?? cash?.getInstance(s.Position);
                if (inst == null || ReferenceEquals(inst, current)) continue;
                if (!IsInstanceHealthy(inst)) continue;
                FillFailoverContext(ctx, current, inst);
                return inst;
            }

            return current;
        }

        private static System.Collections.Generic.IEnumerable<SlaveMember> OrderHotStandbyCandidates(
            MasterSlaveGroup group,
            Func<SlaveMember, TimeSpan?> lagSelector)
        {
            var query = group.Slaves.Where(s => s.HotStandby && s.WriteEnabled);
            if (lagSelector == null)
                return query.OrderBy(s => s.Position);
            return query.OrderBy(s =>
            {
                var lag = lagSelector(s);
                return lag.HasValue ? lag.Value.Ticks : long.MaxValue;
            }).ThenBy(s => s.Position);
        }

        private static void FillFailoverContext(FailoverContext ctx, DBInstance oldInst, DBInstance newInst)
        {
            if (ctx == null) return;
            ctx.OldMaster = oldInst;
            ctx.NewMaster = newInst;
        }

        /// <summary>兼容旧 API。</summary>
        public static DBInstance ElectNewMaster(
            MasterSlaveGroup group,
            MasterSlaveOptions options,
            FailoverContext ctx = null)
            => ElectIfUnavailable(group, null, options, ctx, ctx?.OldMaster);
    }
}