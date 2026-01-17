
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

        public DBInstance DB;

        public LinqDbFactory Factory {  get; set; }

        public EntityQueryProvider EntityProvider
        {
            get {
                return Factory.GetEntityQueryProvider(DB);
            }
        }
    }
}
