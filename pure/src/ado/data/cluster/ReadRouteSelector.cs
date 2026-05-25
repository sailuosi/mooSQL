using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace mooSQL.data.cluster
{
    public static class ReadRouteSelector
    {
        private static readonly ConcurrentDictionary<int, int> _roundRobinByGroup = new ConcurrentDictionary<int, int>();

        public static DBInstance Select(
            MasterSlaveGroup group,
            IList<SlaveMember> candidates,
            ReadRoutePolicy policy,
            Func<MasterSlaveGroup, DBInstance> custom = null)
        {
            if (candidates == null || candidates.Count == 0) return null;
            if (policy == ReadRoutePolicy.Custom && custom != null)
                return custom(group);
            if (policy == ReadRoutePolicy.FirstAvailable)
                return candidates[0].Instance;
            if (policy == ReadRoutePolicy.RoundRobin)
            {
                var groupKey = group?.GroupId ?? 0;
                var next = _roundRobinByGroup.AddOrUpdate(groupKey, 1, (_, v) => v + 1);
                var idx = Math.Abs(next) % candidates.Count;
                return candidates[idx].Instance;
            }
            if (policy == ReadRoutePolicy.WeightedRandom)
            {
                var total = candidates.Sum(c => Math.Max(1, c.Weight));
#if NET6_0_OR_GREATER
                var roll = Random.Shared.Next(total);
#else
                var roll = new Random().Next(total);
#endif
                var acc = 0;
                foreach (var c in candidates)
                {
                    acc += Math.Max(1, c.Weight);
                    if (roll < acc) return c.Instance;
                }
                return candidates[candidates.Count - 1].Instance;
            }
            return candidates[0].Instance;
        }
    }
}
