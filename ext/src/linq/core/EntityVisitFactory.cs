using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data;

using mooSQL.linq;

namespace mooSQL.linq.core
{


    public class EntityVisitFactory : LinqDbFactory
    {
        public override IQueryCompiler GetQueryCompiler(DBInstance DB)
        {
            return new EntityVisitCompiler(DB);
        }
    }
}
