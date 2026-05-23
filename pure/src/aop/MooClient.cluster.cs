using mooSQL.data.cluster;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

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

        internal DBInstance resolveWrite(int position, SQLRouteContext ctx = null) =>
            GetRouteResolver()?.ResolveWrite(position, ctx);

        internal IList<DBInstance> resolveDualWriteTargets(int position, SQLRouteContext ctx = null) =>
            GetRouteResolver()?.ResolveDualWriteTargets(position, ctx) ?? Array.Empty<DBInstance>();

        /// <summary>手动或内部触发 Failover 选举。</summary>
        public DBInstance tryFailover(int groupId) =>
            tryFailoverInternal(groupId, null, "manual");

        internal DBInstance tryFailoverInternal(int groupId, Func<FailoverContext, DBInstance> elector, string trigger)
        {
            if (!_masterSlaveGroups.TryGetValue(groupId, out var group)) return null;
            var ctx = new FailoverContext
            {
                GroupId = groupId,
                Group = group,
                OldMaster = group.GetActiveMaster(),
                Trigger = trigger
            };
            if (elector != null)
            {
                var customOpts = new MasterSlaveOptions
                {
                    CustomFailoverElector = elector,
                    OnFailover = _masterSlaveOptions?.OnFailover
                };
                return FailoverPolicy.ElectNewMaster(group, customOpts, ctx);
            }
            var elected = FailoverPolicy.ElectNewMaster(group, _masterSlaveOptions, ctx);
            if (elected != null)
                events?.FireFailover(ctx);
            return elected;
        }

        /// <summary>手动提升主库（运维回切等）。</summary>
        public void promoteMaster(int groupId, int masterPosition, bool manual = true)
        {
            if (!_masterSlaveGroups.TryGetValue(groupId, out var group)) return;
            var cash = CashHolder;
            if (cash == null) return;
            var db = cash.getInstance(masterPosition);
            group.Master = db;
            if (manual) group.ActiveMaster = db;
        }
    }
}
