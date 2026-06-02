using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data.health;

namespace mooSQL.data.cluster
{
    /// <summary>
    /// DBExecutor 执行作用域路由上下文。
    /// </summary>
    public class SQLRouteContext
    {
        /// <summary>
        /// 字段 TargetPosition（int?）。
        /// </summary>
        public int? TargetPosition;
        /// <summary>
        /// 字段 TargetInstance（DBInstance）。
        /// </summary>
        public DBInstance TargetInstance;

        /// <summary>当次写目标别名，与 <see cref="TargetInstance"/> 同步。</summary>
        public DBInstance WriteTarget
        {
            get => TargetInstance;
            set => TargetInstance = value;
        }

        /// <summary>
        /// 字段 PreferReadReplica（bool?）。
        /// </summary>
        public bool? PreferReadReplica;
        /// <summary>
        /// 字段 ForceMaster（bool?）。
        /// </summary>
        public bool? ForceMaster;
        /// <summary>
        /// 字段 EnableDualWrite（bool?）。
        /// </summary>
        public bool? EnableDualWrite;
        /// <summary>
        /// 字段 FailoverOverride（FailoverMode?）。
        /// </summary>
        public FailoverMode? FailoverOverride;
        /// <summary>
        /// 字段 ReadPolicyOverride（ReadRoutePolicy?）。
        /// </summary>
        public ReadRoutePolicy? ReadPolicyOverride;

        /// <summary>
        /// 字段 DualWritePositions（int[]）。
        /// </summary>
        public int[] DualWritePositions;
        /// <summary>
        /// 自定义读库选择器（覆盖默认读路由策略）。
        /// </summary>
        public Func<MasterSlaveGroup, DBInstance> ReadSelector;
        /// <summary>
        /// 自定义故障转移选举器（覆盖默认 Failover 策略）。
        /// </summary>
        public Func<FailoverContext, DBInstance> FailoverElector;

        /// <summary>
        /// 属性 IsWriteOperation（bool）。
        /// </summary>
        public bool IsWriteOperation { get; set; }

        /// <summary>
        /// Clone 方法（返回 SQLRouteContext）。
        /// </summary>
        public SQLRouteContext Clone()
        {
            return new SQLRouteContext
            {
                TargetPosition = TargetPosition,
                TargetInstance = TargetInstance,
                PreferReadReplica = PreferReadReplica,
                ForceMaster = ForceMaster,
                EnableDualWrite = EnableDualWrite,
                FailoverOverride = FailoverOverride,
                ReadPolicyOverride = ReadPolicyOverride,
                DualWritePositions = DualWritePositions != null ? (int[])DualWritePositions.Clone() : null,
                ReadSelector = ReadSelector,
                FailoverElector = FailoverElector,
                IsWriteOperation = IsWriteOperation
            };
        }
    }

    /// <summary>
    /// 类型 NoReadableReplicaException。
    /// </summary>
    public class NoReadableReplicaException : Exception
    {
        /// <summary>
        /// 初始化 NoReadableReplicaException（构造）。
        /// </summary>
        public NoReadableReplicaException(int groupId)
            : base($"连接位 {groupId} 无可用读从库。") { }
    }

    /// <summary>
    /// 主从路由解析（仅供框架内部与单元测试）。
    /// </summary>
    internal class RouteResolver
    {
        private readonly MooClient _client;
        private readonly DBInsCash _cash;

        public RouteResolver(MooClient client, DBInsCash cash)
        {
            _client = client;
            _cash = cash;
        }

        public MasterSlaveOptions Options => _client?.MasterSlaveOptions ?? new MasterSlaveOptions();

        public MasterSlaveGroup GetGroup(int position)
        {
            return _client?.getGroupInternal(position);
        }

        /// <summary>
        /// 是否对 Select 自动走读从：PreferReadReplica、组 AutoReadReplica 且非 MasterOnly。
        /// </summary>
        public bool ShouldAutoReadReplica(int position, SQLRouteContext ctx)
        {
            if (ctx?.PreferReadReplica == true) return true;
            var group = GetGroup(position);
            if (group == null) return false;
            if (!_client.ResolveAutoReadReplica(position, ctx, group)) return false;
            var policy = ctx?.ReadPolicyOverride ?? GetGroupOverride(position)?.ReadPolicy ?? group.ReadPolicy;
            return policy != ReadRoutePolicy.MasterOnly;
        }

        public DBInstance ResolveRead(int position, SQLRouteContext ctx = null)
        {
            var group = GetGroup(position);
            if (group == null) return _cash.getInstance(position);

            if (ctx?.ForceMaster == true) return ResolveWrite(position, ctx);

            if (ctx?.TargetInstance != null) return ctx.TargetInstance;
            if (ctx?.TargetPosition != null) return _cash.getInstance(ctx.TargetPosition.Value);

            var policy = ctx?.ReadPolicyOverride ?? GetGroupOverride(position)?.ReadPolicy ?? group.ReadPolicy;
            if (policy == ReadRoutePolicy.MasterOnly) return ResolveWritableForRead(group, position, ctx);

            if (ctx?.ReadSelector != null)
            {
                var custom = ctx.ReadSelector(group);
                if (custom != null) return custom;
            }

            if (Options.CustomReadSelector != null)
            {
                var custom = Options.CustomReadSelector(group);
                if (custom != null) return custom;
            }

            var ov = GetGroupOverride(position);
            var candidates = group.Slaves.Where(s => IsReadableCandidate(s, ov) && IsHealthy(s)).ToList();
            if (candidates.Count == 0)
            {
                var fallback = group.ReadFallbackToMaster;
                if (ov?.ReadFallbackToMaster != null) fallback = ov.ReadFallbackToMaster.Value;
                if (fallback)
                    return ResolveWritableForRead(group, position, ctx);
                throw new NoReadableReplicaException(position);
            }

            return ReadRouteSelector.Select(group, candidates, policy, Options.CustomReadSelector)
                   ?? ResolveWritableForRead(group, position, ctx);
        }

        public DBInstance ResolveWrite(int position, SQLRouteContext ctx = null, DBInstance currentHint = null)
        {
            var group = GetGroup(position);
            if (group == null) return _cash.getInstance(position);
            return ResolveWriteInternal(group, position, ctx, currentHint);
        }

        private DBInstance ResolveWriteInternal(MasterSlaveGroup group, int position, SQLRouteContext ctx, DBInstance currentHint)
        {
            if (ctx?.TargetInstance != null) return ctx.TargetInstance;
            if (ctx?.TargetPosition != null) return _cash.getInstance(ctx.TargetPosition.Value);
            if (ctx?.ForceMaster == true) return EnsureWritable(group, position, ctx, currentHint ?? group.Master);

            var current = currentHint ?? group.Master;
            if (FailoverPolicy.IsInstanceHealthy(current)) return current;

            var mode = _client.ResolveFailoverMode(position, ctx, group);
            if (!MooClient.ShouldAttemptFailover(mode))
                return current;

            return _client.electIfUnavailable(position, current, ctx?.FailoverElector, "resolveWrite") ?? current;
        }

        private DBInstance EnsureWritable(MasterSlaveGroup group, int position, SQLRouteContext ctx, DBInstance current)
        {
            if (FailoverPolicy.IsInstanceHealthy(current)) return current;
            var mode = _client.ResolveFailoverMode(position, ctx, group);
            if (!MooClient.ShouldAttemptFailover(mode))
                return current;
            return _client.electIfUnavailable(position, current, ctx?.FailoverElector, "forceMaster") ?? current;
        }

        private DBInstance ResolveWritableForRead(MasterSlaveGroup group, int position, SQLRouteContext ctx)
        {
            var current = group.Master;
            if (FailoverPolicy.IsInstanceHealthy(current)) return current;
            var mode = _client.ResolveFailoverMode(position, ctx, group);
            if (!MooClient.ShouldAttemptFailover(mode))
                return current;
            return _client.electIfUnavailable(position, current, ctx?.FailoverElector, "readFallback") ?? current;
        }

        public IList<DBInstance> ResolveDualWriteTargets(int position, SQLRouteContext ctx = null)
        {
            var group = GetGroup(position);
#if NET451
            if (group == null) return new List<DBInstance>();
#else
            if (group == null) return Array.Empty<DBInstance>();
#endif


            var master = ResolveWrite(position, ctx);
            var list = new List<DBInstance> { master };

            if (ctx?.DualWritePositions != null && ctx.DualWritePositions.Length > 0)
            {
                foreach (var p in ctx.DualWritePositions)
                    list.Add(_cash.getInstance(p));
                return list;
            }

            foreach (var s in group.Slaves.Where(x => x.CanDualWrite && IsHealthy(x)))
                list.Add(s.Instance ?? _cash.getInstance(s.Position));

            return list;
        }

        private GroupOverride GetGroupOverride(int position)
        {
            if (Options.Groups != null && Options.Groups.TryGetValue(position, out var ov))
                return ov;
            return null;
        }

        private static bool IsReadableCandidate(SlaveMember s, GroupOverride ov)
        {
            if (!s.CanRead) return false;
            if (ov?.RequireReadReplica == true && !s.ReadReplica) return false;
            if (ov?.AllowHotStandbyRead == false && s.HotStandby && !s.ReadReplica) return false;
            return true;
        }

        private static bool IsHealthy(SlaveMember s)
        {
            if (s.Health == null) return true;
            return s.Health.Status == DBHealthStatus.Available || s.Health.Status == DBHealthStatus.None;
        }
    }
}