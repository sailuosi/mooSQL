// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.auth
{
    /// <summary>
    /// AuthContext 用于构建认证器，并执行认证操作。
    /// </summary>
    /// <typeparam name="Dialect"></typeparam>
    public class AuthContext<Dialect> where Dialect : AuthDialect
    {
        /// <summary>
        /// 认证器构建者
        /// </summary>
        public AuthorBuilder<Dialect> builder;
        /// <summary>
        /// 认证方言
        /// </summary>
        public Dialect dialect;

        public AuthFactory<Dialect> factory;
    }
}