using System;

namespace mooSQL.data.health
{
    /// <summary>
    /// 单库实例健康状态管理。
    /// </summary>
    public class DBHealth
    {
        private readonly object _lock = new object();

        public DBInstance Owner { get; }
        public DBHealthOptions Options { get; set; }

        /// <summary>测试注入，绕过真实 ping。</summary>
        public Func<DBInstance, bool> PingHandler;

        public DBHealthStatus Status { get; private set; } = DBHealthStatus.None;
        public DateTime? LastSuccessAt { get; private set; }
        public DateTime? LastFailureAt { get; private set; }
        public string LastError { get; private set; }
        public int ConsecutiveFailures { get; private set; }
        public int ProbeAttempts { get; private set; }

        public DBHealth(DBInstance owner, DBHealthOptions options = null)
        {
            Owner = owner;
            Options = options ?? new DBHealthOptions();
        }

        public bool NeedsProbe()
        {
            if (!Options.Enabled) return false;
            if (Status == DBHealthStatus.None) return true;
            if (Status == DBHealthStatus.Unavailable && ProbeAttempts < Options.ReTrySize) return true;
            if (Status == DBHealthStatus.Available && LastSuccessAt.HasValue)
            {
                return DateTime.UtcNow - LastSuccessAt.Value > Options.StaleThreshold;
            }
            return false;
        }

        public bool Probe()
        {
            if (!Options.Enabled) return true;
            lock (_lock)
            {
                SetStatus(DBHealthStatus.Probing);
            }
            try
            {
                var ok = PingHandler != null ? PingHandler(Owner) : DoDefaultPing();
                if (ok) MarkSuccess();
                else MarkFailure(null);
                return ok;
            }
            catch (Exception ex)
            {
                MarkFailure(ex);
                return false;
            }
        }

        public void MarkSuccess()
        {
            lock (_lock)
            {
                ConsecutiveFailures = 0;
                LastSuccessAt = DateTime.UtcNow;
                LastError = null;
                SetStatus(DBHealthStatus.Available);
            }
        }

        public void MarkFailure(Exception ex)
        {
            lock (_lock)
            {
                ConsecutiveFailures++;
                ProbeAttempts++;
                LastFailureAt = DateTime.UtcNow;
                LastError = ex?.Message;
                if (ConsecutiveFailures >= Options.MaxFailures)
                    SetStatus(DBHealthStatus.Unavailable);
            }
        }

        public void markManualRecovery()
        {
            lock (_lock)
            {
                ConsecutiveFailures = 0;
                ProbeAttempts = 0;
            }
            Probe();
        }

        private bool DoDefaultPing()
        {
            if (Owner?.dialect == null) return false;
            var sql = !string.IsNullOrWhiteSpace(Options.CustomPingSQL)
                ? Options.CustomPingSQL
                : Owner.dialect.sentence?.getPingSQL() ?? "SELECT 1";
            var probeExecutor = new DBExecutor(Owner)
            {
                SkipHealthCheck = true,
                ForceUseUnavailable = true
            };
            var dialectMs = Owner.dialect.sentence?.PingTimeoutMs ?? 3000;
            var timeoutMs = Math.Min(Options.PingTimeoutMs, dialectMs);
            var cmd = new SQLCmd(sql, new Paras())
            {
                timeout = Math.Max(1, timeoutMs / 1000)
            };
            Owner.ExeQueryScalar<int>(cmd, probeExecutor);
            return true;
        }

        private void SetStatus(DBHealthStatus next)
        {
            if (Status == next) return;
            var old = Status;
            Status = next;
            Owner?.client?.events?.FireHealthStatusChanged(Owner, old, next);
        }

        /// <summary>单元测试用：模拟探活中间态。</summary>
        internal void ForceStatus(DBHealthStatus next) => SetStatus(next);
    }
}
