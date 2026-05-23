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
        public int? TargetPosition;
        public DBInstance TargetInstance;

        public bool? PreferReadReplica;
        public bool? ForceMaster;
        public bool? EnableDualWrite;
        public FailoverMode? FailoverOverride;
        public ReadRoutePolicy? ReadPolicyOverride;

        public int[] DualWritePositions;
        public Func<MasterSlaveGroup, DBInstance> ReadSelector;
        public Func<FailoverContext, DBInstance> FailoverElector;

        public bool IsWriteOperation { get; set; }

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

    public class NoReadableReplicaException : Exception
    {
        public NoReadableReplicaException(int groupId)
            : base($"连接位 {groupId} 无可用读从库。") { }
    }

    /// <summary>
    /// 主从路由解析。
    /// </summary>
    public class RouteResolver
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

        public DBInstance ResolveRead(int position, SQLRouteContext ctx = null)
        {
            var group = GetGroup(position);
            if (group == null) return _cash.getInstance(position);

            if (ctx?.ForceMaster == true) return ResolveWriteInternal(group, position, ctx);

            if (ctx?.TargetInstance != null) return ctx.TargetInstance;
            if (ctx?.TargetPosition != null) return _cash.getInstance(ctx.TargetPosition.Value);

            var policy = ctx?.ReadPolicyOverride ?? GetGroupOverride(position)?.ReadPolicy ?? group.ReadPolicy;
            if (policy == ReadRoutePolicy.MasterOnly) return group.GetActiveMaster();

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
                var fallback = ov?.ReadPolicy != null ? group.ReadFallbackToMaster : Options.ReadFallbackToMaster;
                if (fallback || group.ReadFallbackToMaster)
                    return group.GetActiveMaster();
                throw new NoReadableReplicaException(position);
            }

            return ReadRouteSelector.Select(group, candidates, policy, Options.CustomReadSelector)
                   ?? group.GetActiveMaster();
        }

        public DBInstance ResolveWrite(int position, SQLRouteContext ctx = null)
        {
            var group = GetGroup(position);
            if (group == null) return _cash.getInstance(position);
            return ResolveWriteInternal(group, position, ctx);
        }

        private DBInstance ResolveWriteInternal(MasterSlaveGroup group, int position, SQLRouteContext ctx)
        {
            if (ctx?.TargetInstance != null) return ctx.TargetInstance;
            if (ctx?.TargetPosition != null) return _cash.getInstance(ctx.TargetPosition.Value);
            if (ctx?.ForceMaster == true) return EnsureWritableMaster(group, position, ctx);

            var master = group.GetActiveMaster();
            if (IsInstanceHealthy(master)) return master;

            var mode = ctx?.FailoverOverride ?? GetGroupOverride(position)?.Failover ?? group.FailoverMode;
            if (mode == FailoverMode.Disabled || mode == FailoverMode.MarkOnly)
                return master;

            return _client.tryFailoverInternal(position, ctx?.FailoverElector, "resolveWrite") ?? master;
        }

        public IList<DBInstance> ResolveDualWriteTargets(int position, SQLRouteContext ctx = null)
        {
            var group = GetGroup(position);
            if (group == null) return Array.Empty<DBInstance>();

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

        private DBInstance EnsureWritableMaster(MasterSlaveGroup group, int position, SQLRouteContext ctx)
        {
            var master = group.GetActiveMaster();
            if (IsInstanceHealthy(master)) return master;
            return _client.tryFailoverInternal(position, ctx?.FailoverElector, "forceMaster") ?? master;
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

        private static bool IsInstanceHealthy(DBInstance db)
        {
            if (db?.Health == null) return true;
            return db.Health.Status == DBHealthStatus.Available || db.Health.Status == DBHealthStatus.None;
        }
    }
}
