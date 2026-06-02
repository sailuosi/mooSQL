using System;

namespace mooSQL.data.health
{
    /// <summary>
    /// 单库实例健康状态管理。
    /// </summary>
    public class DBHealth
    {
        private readonly object _lock = new object();

        /// <summary>
        /// 属性 Owner（DBInstance）。
        /// </summary>
        public DBInstance Owner { get; }
        /// <summary>
        /// 属性 Options（DBHealthOptions）。
        /// </summary>
        public DBHealthOptions Options { get; set; }

        /// <summary>测试注入，绕过真实 ping。</summary>
        public Func<DBInstance, bool> PingHandler;

        /// <summary>
        /// 属性 Status（DBHealthStatus）。
        /// </summary>
        public DBHealthStatus Status { get; private set; } = DBHealthStatus.None;
        /// <summary>
        /// 属性 LastSuccessAt（DateTime?）。
        /// </summary>
        public DateTime? LastSuccessAt { get; private set; }
        /// <summary>
        /// 属性 LastFailureAt（DateTime?）。
        /// </summary>
        public DateTime? LastFailureAt { get; private set; }
        /// <summary>
        /// 属性 LastError（string）。
        /// </summary>
        public string LastError { get; private set; }
        /// <summary>
        /// 属性 ConsecutiveFailures（int）。
        /// </summary>
        public int ConsecutiveFailures { get; private set; }
        /// <summary>
        /// 属性 ProbeAttempts（int）。
        /// </summary>
        public int ProbeAttempts { get; private set; }

        /// <summary>
        /// 初始化 DBHealth（构造）。
        /// </summary>
        public DBHealth(DBInstance owner, DBHealthOptions options = null)
        {
            Owner = owner;
            Options = options ?? new DBHealthOptions();
        }

        /// <summary>
        /// NeedsProbe 方法（返回 bool）。
        /// </summary>
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

        /// <summary>
        /// Probe 方法（返回 bool）。
        /// </summary>
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

        /// <summary>
        /// MarkSuccess 方法。
        /// </summary>
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

        /// <summary>
        /// MarkFailure 方法。
        /// </summary>
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

        /// <summary>
        /// markManualRecovery 方法。
        /// </summary>
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
            var sql =Options.CustomPingSQL.HasText()
                ? Options.CustomPingSQL
                : Owner.dialect.sentence?.getPingSQL();
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