using mooSQL.data.health;

namespace mooSQL.data.cluster
{
    /// <summary>
    /// 从库成员，bool 能力可组合。
    /// </summary>
    public class SlaveMember
    {
        public int Position;
        public DBInstance Instance;
        public int Weight = 1;
        public bool ReadEnabled = true;
        public bool WriteEnabled;

        public bool ReadReplica;
        public bool HotStandby;
        public bool DualWrite;
        public bool AsyncReplica;

        public DBHealth Health => Instance?.Health;

        public bool CanRead => ReadEnabled && (ReadReplica || HotStandby);

        public bool CanFailover =>
            HotStandby && WriteEnabled &&
            (Health == null || Health.Status == DBHealthStatus.Available || Health.Status == DBHealthStatus.None);

        public bool CanDualWrite => DualWrite && WriteEnabled;
    }
}
