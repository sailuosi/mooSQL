using mooSQL.data.health;

namespace mooSQL.data
{
    public partial class DBInstance
    {
        private DBHealth _health;

        /// <summary>
        /// 实例健康状态；未启用时为 null。
        /// </summary>
        public DBHealth Health
        {
            get => _health;
            set => _health = value;
        }

        /// <summary>
        /// 初始化或获取健康组件。
        /// </summary>
        public DBHealth EnsureHealth(DBHealthOptions options = null)
        {
            if (_health == null)
            {
                var opt = options ?? config?.healthOptions ?? new DBHealthOptions();
                _health = new DBHealth(this, opt);
            }
            else if (options != null)
            {
                _health.Options = options;
            }
            return _health;
        }
    }
}
