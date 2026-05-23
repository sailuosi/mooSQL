using System;
using System.Collections.Generic;
using System.Threading;

namespace mooSQL.data.health
{
    /// <summary>
    /// 对 Unavailable 实例周期探活。
    /// </summary>
    public class HealthProbeScheduler : IDisposable
    {
        private readonly Timer _timer;
        private readonly List<DBHealth> _targets = new List<DBHealth>();
        private readonly object _lock = new object();

        public HealthProbeScheduler(TimeSpan? interval = null)
        {
            var ms = (int)(interval ?? TimeSpan.FromSeconds(30)).TotalMilliseconds;
            _timer = new Timer(_ => Tick(), null, ms, ms);
        }

        public void Register(DBHealth health)
        {
            if (health == null) return;
            lock (_lock)
            {
                if (!_targets.Contains(health)) _targets.Add(health);
            }
        }

        public void Unregister(DBHealth health)
        {
            lock (_lock) _targets.Remove(health);
        }

        private void Tick()
        {
            DBHealth[] snapshot;
            lock (_lock) snapshot = _targets.ToArray();
            foreach (var h in snapshot)
            {
                if (h.Status != DBHealthStatus.Unavailable) continue;
                if (h.ProbeAttempts >= h.Options.ReTrySize) continue;
                h.Probe();
            }
        }

        public void Dispose() => _timer?.Dispose();
    }
}
