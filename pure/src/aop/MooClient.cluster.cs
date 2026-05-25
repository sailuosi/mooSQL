using mooSQL.data.cluster;
using mooSQL.data.health;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace mooSQL.data
{
    public partial class MooClient
    {
        private readonly ConcurrentDictionary<int, MasterSlaveGroup> _masterSlaveGroups =
            new ConcurrentDictionary<int, MasterSlaveGroup>();

        private MasterSlaveOptions _masterSlaveOptions = new MasterSlaveOptions();
        private RouteResolver _routeResolver;

        /// <summary>主从路由全局默认与按组覆盖。</summary>
        public MasterSlaveOptions MasterSlaveOptions
        {
            get => _masterSlaveOptions;
            set
            {
                _masterSlaveOptions = value ?? new MasterSlaveOptions();
                _routeResolver = null;
            }
        }

        internal RouteResolver GetRouteResolver()
        {
            var cash = CashHolder;
            if (cash == null) return null;
            return _routeResolver ?? (_routeResolver = new RouteResolver(this, cash));
        }

        /// <summary>注册或更新主从组。</summary>
        public void configureGroup(int masterPosition, Action<GroupBuilder> setup, MasterSlaveOptions options = null)
        {
            if (options != null) MasterSlaveOptions = options;
            var cash = CashHolder;
            if (cash == null)
                throw new InvalidOperationException("MooClient.CashHolder 未设置，无法注册主从组。");
            var builder = new GroupBuilder(cash, masterPosition);
            setup?.Invoke(builder);
            _masterSlaveGroups[masterPosition] = builder.Build();
        }

        /// <summary>获取连接位对应的主从组。</summary>
        public MasterSlaveGroup getGroup(int position) => getGroupInternal(position);

        internal MasterSlaveGroup getGroupInternal(int position)
        {
            _masterSlaveGroups.TryGetValue(position, out var g);
            return g;
        }

        internal DBInstance resolveRead(int position, SQLRouteContext ctx = null) =>
            GetRouteResolver()?.ResolveRead(position, ctx);

        internal DBInstance resolveWrite(int position, SQLRouteContext ctx = null, DBInstance currentHint = null) =>
            GetRouteResolver()?.ResolveWrite(position, ctx, currentHint);

        internal IList<DBInstance> resolveDualWriteTargets(int position, SQLRouteContext ctx = null) =>
            GetRouteResolver()?.ResolveDualWriteTargets(position, ctx) ?? Array.Empty<DBInstance>();

        /// <summary>当前实例不可用则选举下一可用可写实例（不写回组级状态）。</summary>
        public DBInstance electIfUnavailable(int groupId, DBInstance currentFailed, Func<FailoverContext, DBInstance> elector = null, string trigger = "manual")
        {
            if (!_masterSlaveGroups.TryGetValue(groupId, out var group)) return currentFailed;
            var ctx = new FailoverContext
            {
                GroupId = groupId,
                Group = group,
                OldMaster = currentFailed,
                Trigger = trigger
            };

            MasterSlaveOptions opts;
            if (elector != null)
            {
                opts = new MasterSlaveOptions
                {
                    CustomFailoverElector = elector,
                    OnFailover = _masterSlaveOptions?.OnFailover
                };
            }
            else
            {
                opts = _masterSlaveOptions;
            }

            var elected = FailoverPolicy.ElectIfUnavailable(group, CashHolder, opts, ctx, currentFailed);
            if (elected != null
                && currentFailed != null
                && !ReferenceEquals(elected, currentFailed)
                && FailoverPolicy.IsInstanceHealthy(elected))
            {
                events?.FireFailover(ctx);
            }

            return elected;
        }

        /// <summary>手动或内部触发 Failover 选举。</summary>
        public DBInstance tryFailover(int groupId) =>
            electIfUnavailable(groupId, null, null, "manual");

        internal DBInstance tryFailoverInternal(int groupId, DBInstance currentFailed, Func<FailoverContext, DBInstance> elector, string trigger) =>
            electIfUnavailable(groupId, currentFailed, elector, trigger);

        /// <summary>手动调整配置主（运维回切等），不维护组级 ActiveMaster。</summary>
        public void promoteMaster(int groupId, int masterPosition, bool manual = true)
        {
            if (!_masterSlaveGroups.TryGetValue(groupId, out var group)) return;
            var cash = CashHolder;
            if (cash == null) return;
            group.Master = cash.getInstance(masterPosition);
        }

        internal static bool ShouldAttemptFailover(FailoverMode mode) =>
            mode != FailoverMode.Disabled && mode != FailoverMode.MarkOnly;

        internal FailoverMode ResolveFailoverMode(int position, SQLRouteContext ctx, MasterSlaveGroup group)
        {
            if (ctx?.FailoverOverride != null) return ctx.FailoverOverride.Value;
            if (_masterSlaveOptions?.Groups != null
                && _masterSlaveOptions.Groups.TryGetValue(position, out var ov)
                && ov.Failover != null)
                return ov.Failover.Value;
            return group?.FailoverMode ?? FailoverMode.Disabled;
        }

        /// <summary>解析实例所属主从组 ID（配置主连接位）。</summary>
        internal int resolveGroupIdFor(DBInstance db)
        {
            if (db == null) return -1;
            var idx = db.config?.index ?? -1;
            foreach (var kv in _masterSlaveGroups)
            {
                var g = kv.Value;
                if (g.Master != null && g.Master.config?.index == idx) return kv.Key;
                if (g.Slaves.Any(s => s.Position == idx || (s.Instance != null && s.Instance.config?.index == idx)))
                    return kv.Key;
            }
            return idx;
        }
    }
}
