using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.config
{
    /// <summary>
    /// 数据库连接配置，基于常规概念的，而不是成型的数据库连接字符串。对于不同的数据库，可能有不同的参数
    /// </summary>
    public class DBBuildInfo
    {

        /// <summary>
        /// 主机名
        /// </summary>
        public string Host;
        /// <summary>
        /// 端口
        /// </summary>
        public int Port;
        /// <summary>
        /// 用户名
        /// </summary>
        public string UserID;
        /// <summary>
        /// 密码
        /// </summary>
        public string PassWord;
        /// <summary>
        /// 要连接的数据库名称
        /// </summary>
        public string DataBase;
        /// <summary>
        /// 使用连接池
        /// </summary>
        public bool? Pooling;

        public int? MiniPoolSize;

        public int? MaxPoolSize;
        /// <summary>
        /// 附加自定义配置
        /// </summary>
        public string Append;
    }
}
