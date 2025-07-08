using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.config
{
    /// <summary>
    /// 可供查询直接使用的，数据库配置信息
    /// </summary>
    public class DBPosition
    {
        /// <summary>
        /// 连接位的索引
        /// </summary>
        public int Position { get; set; }
        /// <summary>
        /// 连接位的名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 数据库类型
        /// </summary>
        public string DbType { get; set; }
        /// <summary>
        /// 连接字符串
        /// </summary>
        public string ConnectString { get; set; }
        /// <summary>
        /// 数据库版本
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 版本号
        /// </summary>
        public double? VersionNumber{ get; set; }
    }
}
