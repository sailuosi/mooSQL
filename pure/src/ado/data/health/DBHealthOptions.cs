using System;

namespace mooSQL.data.health
{
    /// <summary>
    /// 实例健康探测配置。
    /// </summary>
    public class DBHealthOptions
    {
        public bool Enabled = true;
        public int MaxFailures = 3;
        public int ReTrySize = 10;
        public TimeSpan RecoveryInterval = TimeSpan.FromSeconds(30);
        public TimeSpan StaleThreshold = TimeSpan.FromMinutes(5);
        public string CustomPingSQL;
        /// <summary>
        /// 探活超时毫秒，默认 3000。
        /// </summary>
        public int PingTimeoutMs = 3000;
    }
}
