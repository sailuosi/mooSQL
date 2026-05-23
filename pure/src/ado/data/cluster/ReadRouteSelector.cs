using System;
using System.Collections.Generic;
using System.Linq;

namespace mooSQL.data.cluster
{
    public static class ReadRouteSelector
    {
        private static int _roundRobin;

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
                var idx = Math.Abs(System.Threading.Interlocked.Increment(ref _roundRobin)) % candidates.Count;
                return candidates[idx].Instance;
            }
            if (policy == ReadRoutePolicy.WeightedRandom)
            {
                var total = candidates.Sum(c => Math.Max(1, c.Weight));
                var roll = new Random().Next(total);
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
