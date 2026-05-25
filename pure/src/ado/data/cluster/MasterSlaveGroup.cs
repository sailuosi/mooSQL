using System;
using System.Collections.Generic;

namespace mooSQL.data.cluster
{
    public class MasterSlaveGroup
    {
        public int GroupId;
        /// <summary>配置锚点（组 ID / 默认连接位），不隐含唯一写库。</summary>
        public DBInstance Master;
        public List<SlaveMember> Slaves = new List<SlaveMember>();
        public FailoverMode FailoverMode = FailoverMode.OnNextConnect;
        public ReadRoutePolicy ReadPolicy = ReadRoutePolicy.WeightedRandom;
        public bool ReadFallbackToMaster = true;
        /// <summary>为 true 且 ReadPolicy 非 MasterOnly 时，Select 自动走读从库（默认 false，迁移见 doc/slave）。</summary>
        public bool AutoReadReplica;
    }

    public class GroupOverride
    {
        public FailoverMode? Failover;
        public ReadRoutePolicy? ReadPolicy;
        public bool? DualWriteSync;
        public bool? RequireReadReplica;
        public bool? AllowHotStandbyRead;
        public bool? AutoReadReplica;
        public bool? ReadFallbackToMaster;
    }

    public class MasterSlaveOptions
    {
        public FailoverMode DefaultFailover = FailoverMode.OnNextConnect;
        public ReadRoutePolicy DefaultReadPolicy = ReadRoutePolicy.WeightedRandom;
        public bool ReadFallbackToMaster = true;
        public DualWriteErrorPolicy DualWriteError = DualWriteErrorPolicy.MasterWins;

        public Dictionary<int, GroupOverride> Groups = new Dictionary<int, GroupOverride>();

        public Func<MasterSlaveGroup, DBInstance> CustomReadSelector;
        public Func<FailoverContext, DBInstance> CustomFailoverElector;
        public Action<FailoverContext> OnFailover;
        /// <summary>热备选举时按复制延迟升序；返回 null 表示未知延迟（排后）。</summary>
        public Func<SlaveMember, TimeSpan?> GetReplicationLag;
    }

    public class FailoverContext
    {
        public int GroupId;
        public DBInstance OldMaster;
        public DBInstance NewMaster;
        public string Trigger;
        public MasterSlaveGroup Group;
    }
}
