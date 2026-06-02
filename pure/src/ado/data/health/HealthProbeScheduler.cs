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

        /// <summary>调度器扫描间隔（各实例按 <see cref="DBHealthOptions.RecoveryInterval"/> 决定是否探活）。</summary>
        public HealthProbeScheduler(TimeSpan? tickInterval = null)
        {
            var ms = (int)(tickInterval ?? TimeSpan.FromSeconds(5)).TotalMilliseconds;
            _timer = new Timer(_ => Tick(), null, ms, ms);
        }

        /// <summary>
        /// Register 方法。
        /// </summary>
        public void Register(DBHealth health)
        {
            if (health == null) return;
            lock (_lock)
            {
                if (!_targets.Contains(health)) _targets.Add(health);
            }
        }

        /// <summary>
        /// Unregister 方法。
        /// </summary>
        public void Unregister(DBHealth health)
        {
            lock (_lock) _targets.Remove(health);
        }

        private void Tick()
        {
            DBHealth[] snapshot;
            lock (_lock) snapshot = _targets.ToArray();
            var now = DateTime.UtcNow;
            foreach (var h in snapshot)
            {
                if (h.Status != DBHealthStatus.Unavailable) continue;
                if (h.ProbeAttempts >= h.Options.ReTrySize) continue;
                if (!h.LastFailureAt.HasValue
                    || now - h.LastFailureAt.Value < h.Options.RecoveryInterval)
                    continue;
                h.Probe();
            }
        }

        /// <summary>
        /// Dispose 方法。
        /// </summary>
        public void Dispose() => _timer?.Dispose();
    }
}