
using mooSQL.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    /// <summary>
    /// 代表一个数据库会话，可用于查询和保存实体
    /// </summary>
    public class DbContext
    {

        /// <summary>底层数据库访问实例。</summary>
        public DBInstance DB;

        /// <summary>LINQ 查询编译与实体提供程序的工厂。</summary>
        public LinqDbFactory Factory {  get; set; }

        /// <summary>当前库对应的实体查询提供程序（由 <see cref="Factory"/> 创建）。</summary>
        public EntityQueryProvider EntityProvider
        {
            get {
                return Factory.GetEntityQueryProvider(DB);
            }
        }
    }
}
