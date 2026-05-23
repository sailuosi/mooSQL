using mooSQL.data.cluster;
using mooSQL.data.health;
using System;

namespace mooSQL.data
{
    public partial class DBInsCash
    {
        private HealthProbeScheduler _healthScheduler;

        public HealthProbeScheduler HealthScheduler
        {
            get
            {
                if (_healthScheduler == null)
                    _healthScheduler = new HealthProbeScheduler();
                return _healthScheduler;
            }
        }

        /// <summary>注册主从组（委托 <see cref="MooClient"/>）。</summary>
        public void configureGroup(int masterPosition, Action<GroupBuilder> setup, MasterSlaveOptions options = null)
        {
            getClient().configureGroup(masterPosition, setup, options);
        }

        /// <summary>获取主从组（委托 <see cref="MooClient"/>）。</summary>
        public MasterSlaveGroup getGroup(int position) => getClient().getGroup(position);
    }
}
