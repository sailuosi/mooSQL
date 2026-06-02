using System;
using System.Collections.Generic;

namespace mooSQL.data.cluster
{
    /// <summary>
    /// 类型 MasterSlaveGroup。
    /// </summary>
    public class MasterSlaveGroup
    {
        /// <summary>
        /// 字段 GroupId（int）。
        /// </summary>
        public int GroupId;
        /// <summary>配置锚点（组 ID / 默认连接位），不隐含唯一写库。</summary>
        public DBInstance Master;
        /// <summary>
        /// 字段 Slaves（List<SlaveMember>）。
        /// </summary>
        public List<SlaveMember> Slaves = new List<SlaveMember>();
        /// <summary>
        /// 字段 FailoverMode（FailoverMode）。
        /// </summary>
        public FailoverMode FailoverMode = FailoverMode.OnNextConnect;
        /// <summary>
        /// 字段 ReadPolicy（ReadRoutePolicy）。
        /// </summary>
        public ReadRoutePolicy ReadPolicy = ReadRoutePolicy.WeightedRandom;
        /// <summary>
        /// 字段 ReadFallbackToMaster（bool）。
        /// </summary>
        public bool ReadFallbackToMaster = true;
        /// <summary>为 true 且 ReadPolicy 非 MasterOnly 时，Select 自动走读从库（默认 false，迁移见 doc/slave）。</summary>
        public bool AutoReadReplica;
    }

    /// <summary>
    /// 类型 GroupOverride。
    /// </summary>
    public class GroupOverride
    {
        /// <summary>
        /// 字段 Failover（FailoverMode?）。
        /// </summary>
        public FailoverMode? Failover;
        /// <summary>
        /// 字段 ReadPolicy（ReadRoutePolicy?）。
        /// </summary>
        public ReadRoutePolicy? ReadPolicy;
        /// <summary>
        /// 字段 DualWriteSync（bool?）。
        /// </summary>
        public bool? DualWriteSync;
        /// <summary>
        /// 字段 RequireReadReplica（bool?）。
        /// </summary>
        public bool? RequireReadReplica;
        /// <summary>
        /// 字段 AllowHotStandbyRead（bool?）。
        /// </summary>
        public bool? AllowHotStandbyRead;
        /// <summary>
        /// 字段 AutoReadReplica（bool?）。
        /// </summary>
        public bool? AutoReadReplica;
        /// <summary>
        /// 字段 ReadFallbackToMaster（bool?）。
        /// </summary>
        public bool? ReadFallbackToMaster;
    }

    /// <summary>
    /// 类型 MasterSlaveOptions。
    /// </summary>
    public class MasterSlaveOptions
    {
        /// <summary>
        /// 字段 DefaultFailover（FailoverMode）。
        /// </summary>
        public FailoverMode DefaultFailover = FailoverMode.OnNextConnect;
        /// <summary>
        /// 字段 DefaultReadPolicy（ReadRoutePolicy）。
        /// </summary>
        public ReadRoutePolicy DefaultReadPolicy = ReadRoutePolicy.WeightedRandom;
        /// <summary>
        /// 字段 ReadFallbackToMaster（bool）。
        /// </summary>
        public bool ReadFallbackToMaster = true;
        /// <summary>
        /// 字段 DualWriteError（DualWriteErrorPolicy）。
        /// </summary>
        public DualWriteErrorPolicy DualWriteError = DualWriteErrorPolicy.MasterWins;

        /// <summary>
        /// 按组号覆盖路由策略的字典。
        /// </summary>
        public Dictionary<int, GroupOverride> Groups = new Dictionary<int, GroupOverride>();

        /// <summary>
        /// 全局自定义读库选择委托。
        /// </summary>
        public Func<MasterSlaveGroup, DBInstance> CustomReadSelector;
        /// <summary>
        /// 全局自定义故障转移选举委托。
        /// </summary>
        public Func<FailoverContext, DBInstance> CustomFailoverElector;
        /// <summary>
        /// 字段 OnFailover（Action<FailoverContext>）。
        /// </summary>
        public Action<FailoverContext> OnFailover;
        /// <summary>热备选举时按复制延迟升序；返回 null 表示未知延迟（排后）。</summary>
        public Func<SlaveMember, TimeSpan?> GetReplicationLag;
    }

    /// <summary>
    /// 类型 FailoverContext。
    /// </summary>
    public class FailoverContext
    {
        /// <summary>
        /// 字段 GroupId（int）。
        /// </summary>
        public int GroupId;
        /// <summary>
        /// 字段 OldMaster（DBInstance）。
        /// </summary>
        public DBInstance OldMaster;
        /// <summary>
        /// 字段 NewMaster（DBInstance）。
        /// </summary>
        public DBInstance NewMaster;
        /// <summary>
        /// 字段 Trigger（string）。
        /// </summary>
        public string Trigger;
        /// <summary>
        /// 字段 Group（MasterSlaveGroup）。
        /// </summary>
        public MasterSlaveGroup Group;
    }
}