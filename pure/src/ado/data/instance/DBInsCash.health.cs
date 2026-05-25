using mooSQL.data.cluster;
using mooSQL.data.health;

namespace mooSQL.data
{
    public partial class DBInsCash
    {
        partial void OnInstanceBuilt(DBInstance db)
        {
            if (db.config?.healthOptions != null)
                db.EnsureHealth(db.config.healthOptions);
            else
                db.EnsureHealth();

            if (db.Health != null)
                HealthScheduler.Register(db.Health);
        }

        partial void OnInstanceRetrieved(DBInstance db)
        {
            var h = db?.Health;
            if (h == null || !h.Options.Enabled) return;
            if (h.Status == DBHealthStatus.Unavailable) return;
            if (h.Status == DBHealthStatus.None || h.NeedsProbe())
            {
                try { h.Probe(); } catch { /* lazy probe best effort */ }
            }
        }

        /// <summary>下发实例前：病态且策略允许时超前选举下一可用实例。</summary>
        internal DBInstance ApplyProactiveFailoverIfNeeded(DBInstance db, int position)
        {
            if (db == null) return null;
            var c = this.client ?? getClient();
            if (c == null) return db;

            var group = c.getGroupInternal(position);
            if (group == null) return db;

            if (db.Health?.Status != DBHealthStatus.Unavailable) return db;

            var mode = c.ResolveFailoverMode(position, null, group);
            if (!MooClient.ShouldAttemptFailover(mode)) return db;

            var elected = c.electIfUnavailable(position, db, null, "getInstance");
            return elected ?? db;
        }
    }
}
