using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data;


namespace mooSQL.linq
{
    /// <summary>
    /// 使用快速表达式编译器的 LINQ 工厂实现。
    /// </summary>
    public class FastLinqFactory : LinqDbFactory
    {
        /// <inheritdoc />
        public override IQueryCompiler GetQueryCompiler(DBInstance DB)
        {
            return new FastLinqCompiler(DB);
        }
    }
}
