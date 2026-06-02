using mooSQL.data.health;

namespace mooSQL.data.cluster
{
    /// <summary>
    /// 从库成员，bool 能力可组合。
    /// </summary>
    public class SlaveMember
    {
        /// <summary>
        /// 字段 Position（int）。
        /// </summary>
        public int Position;
        /// <summary>
        /// 字段 Instance（DBInstance）。
        /// </summary>
        public DBInstance Instance;
        /// <summary>
        /// 字段 Weight（int）。
        /// </summary>
        public int Weight = 1;
        /// <summary>
        /// 字段 ReadEnabled（bool）。
        /// </summary>
        public bool ReadEnabled = true;
        /// <summary>
        /// 字段 WriteEnabled（bool）。
        /// </summary>
        public bool WriteEnabled;

        /// <summary>
        /// 字段 ReadReplica（bool）。
        /// </summary>
        public bool ReadReplica;
        /// <summary>
        /// 字段 HotStandby（bool）。
        /// </summary>
        public bool HotStandby;
        /// <summary>
        /// 字段 DualWrite（bool）。
        /// </summary>
        public bool DualWrite;
        /// <summary>
        /// 字段 AsyncReplica（bool）。
        /// </summary>
        public bool AsyncReplica;

        /// <summary>
        /// 字段 Health（DBHealth）。
        /// </summary>
        public DBHealth Health => Instance?.Health;

        /// <summary>
        /// 字段 CanRead（bool）。
        /// </summary>
        public bool CanRead => ReadEnabled && (ReadReplica || HotStandby);

        /// <summary>
        /// 字段 CanFailover（bool）。
        /// </summary>
        public bool CanFailover =>
            HotStandby && WriteEnabled &&
            (Health == null || Health.Status == DBHealthStatus.Available || Health.Status == DBHealthStatus.None);

        /// <summary>
        /// 字段 CanDualWrite（bool）。
        /// </summary>
        public bool CanDualWrite => DualWrite && WriteEnabled;
    }
}