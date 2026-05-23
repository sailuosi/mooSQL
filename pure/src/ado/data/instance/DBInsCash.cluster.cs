using mooSQL.data.cluster;
using mooSQL.data.health;
using System;
using System.Collections.Concurrent;

namespace mooSQL.data
{
    public partial class DBInsCash
    {
        private readonly ConcurrentDictionary<int, MasterSlaveGroup> _groups =
            new ConcurrentDictionary<int, MasterSlaveGroup>();

        private MasterSlaveOptions _masterSlaveOptions = new MasterSlaveOptions();
        private RouteResolver _routeResolver;
        private HealthProbeScheduler _healthScheduler;

        public MasterSlaveOptions MasterSlaveOptions
        {
            get => _masterSlaveOptions;
            set
            {
                _masterSlaveOptions = value ?? new MasterSlaveOptions();
                _routeResolver = new RouteResolver(this, _masterSlaveOptions);
            }
        }

        public HealthProbeScheduler HealthScheduler
        {
            get
            {
                if (_healthScheduler == null)
                    _healthScheduler = new HealthProbeScheduler();
                return _healthScheduler;
            }
        }

        private RouteResolver Resolver =>
            _routeResolver ?? (_routeResolver = new RouteResolver(this, _masterSlaveOptions));

        public void configureGroup(int masterPosition, Action<GroupBuilder> setup, MasterSlaveOptions options = null)
        {
            if (options != null) MasterSlaveOptions = options;
            var builder = new GroupBuilder(this, masterPosition);
            setup?.Invoke(builder);
            var group = builder.Build();
            _groups[masterPosition] = group;
        }

        public MasterSlaveGroup getGroup(int position) => getGroupInternal(position);

        internal MasterSlaveGroup getGroupInternal(int position)
        {
            _groups.TryGetValue(position, out var g);
            return g;
        }

        public DBInstance GetRead(int position) => Resolver.ResolveRead(position);

        public DBInstance GetWrite(int position) => Resolver.ResolveWrite(position);

        public DBInstance resolveRead(int position, SQLRouteContext ctx = null) =>
            Resolver.ResolveRead(position, ctx);

        public DBInstance resolveWrite(int position, SQLRouteContext ctx = null) =>
            Resolver.ResolveWrite(position, ctx);

        public System.Collections.Generic.IList<DBInstance> resolveDualWriteTargets(int position, SQLRouteContext ctx = null) =>
            Resolver.ResolveDualWriteTargets(position, ctx);

        public DBInstance tryFailover(int groupId) =>
            tryFailoverInternal(groupId, null, "manual");

        internal DBInstance tryFailoverInternal(int groupId, Func<FailoverContext, DBInstance> elector, string trigger)
        {
            if (!_groups.TryGetValue(groupId, out var group)) return null;
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
                client?.events?.FireFailover(ctx);
            return elected;
        }

        public void promoteMaster(int groupId, int masterPosition, bool manual = true)
        {
            if (!_groups.TryGetValue(groupId, out var group)) return;
            var db = getInstance(masterPosition);
            group.Master = db;
            if (manual) group.ActiveMaster = db;
        }
    }
}
