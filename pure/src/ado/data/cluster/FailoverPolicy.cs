using System.Linq;
using mooSQL.data.health;

namespace mooSQL.data.cluster
{
    public static class FailoverPolicy
    {
        public static DBInstance ElectNewMaster(
            MasterSlaveGroup group,
            MasterSlaveOptions options,
            FailoverContext ctx = null)
        {
            if (group == null) return null;

            if (options?.CustomFailoverElector != null && ctx != null)
            {
                var custom = options.CustomFailoverElector(ctx);
                if (custom != null)
                {
                    group.ActiveMaster = custom;
                    return custom;
                }
            }

            var candidates = group.Slaves
                .Where(s => s.HotStandby && s.CanFailover)
                .OrderBy(s => s.Position)
                .ToList();

            if (candidates.Count == 0) return null;

            var elected = candidates[0].Instance;
            var old = group.GetActiveMaster();
            group.ActiveMaster = elected;

            if (options?.OnFailover != null && ctx != null)
            {
                ctx.OldMaster = old;
                ctx.NewMaster = elected;
                options.OnFailover(ctx);
            }

            return elected;
        }
    }
}
