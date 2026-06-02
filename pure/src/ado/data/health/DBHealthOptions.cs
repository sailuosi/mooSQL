using System;

namespace mooSQL.data.health
{
    /// <summary>
    /// 实例健康探测配置。
    /// </summary>
    public class DBHealthOptions
    {
        /// <summary>
        /// 字段 Enabled（bool）。
        /// </summary>
        public bool Enabled = true;
        /// <summary>
        /// 字段 MaxFailures（int）。
        /// </summary>
        public int MaxFailures = 3;
        /// <summary>
        /// 字段 ReTrySize（int）。
        /// </summary>
        public int ReTrySize = 10;
        /// <summary>
        /// 字段 RecoveryInterval（TimeSpan）。
        /// </summary>
        public TimeSpan RecoveryInterval = TimeSpan.FromSeconds(30);
        /// <summary>
        /// 字段 StaleThreshold（TimeSpan）。
        /// </summary>
        public TimeSpan StaleThreshold = TimeSpan.FromMinutes(5);
        /// <summary>
        /// 字段 CustomPingSQL（string）。
        /// </summary>
        public string CustomPingSQL;
        /// <summary>
        /// 探活超时毫秒，默认 3000。
        /// </summary>
        public int PingTimeoutMs = 3000;
    }
}