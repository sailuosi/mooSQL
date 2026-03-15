using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data;

namespace mooSQL.linq
{
    /// <summary>
    /// 实体查询的LINQ，基于本工厂的DbBus，都是基于实体类查询的
    /// </summary>
    public class EntityLinqFactory : LinqDbFactory
    {
        public override IQueryCompiler GetQueryCompiler(DBInstance DB)
        {
            return new EntityQueryCompiler(DB);
        }
    }
}
