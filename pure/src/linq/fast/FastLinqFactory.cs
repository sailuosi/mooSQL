using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data;


namespace mooSQL.linq
{
    public class FastLinqFactory : LinqDbFactory
    {
        public override IQueryCompiler GetQueryCompiler(DBInstance DB)
        {
            return new FastLinqCompiler(DB);
        }
    }
}
