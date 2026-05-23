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
            if (h.Status == DBHealthStatus.None || h.NeedsProbe())
            {
                try { h.Probe(); } catch { /* lazy probe best effort */ }
            }
        }
    }
}
