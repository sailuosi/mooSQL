using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model
{
    /// <summary>
    /// 代表一个SQL片段，也可以是一个完整的SQL
    /// </summary>
    public class SQLFragClause:Clause
    {
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitSQLFrag(this);
        }

        public string SQL;

        public Paras Para;
        public SQLFragClause(string SQL) : base(ClauseType.SQLFrag, null)
        {
            this.SQL = SQL;
        }
        public SQLFragClause(string SQL, Paras Para) : base(ClauseType.SQLFrag, null)
        { 
            this.Para = Para;
            this.SQL = SQL;
        }

        public override string ToString()
        {
            return this.SQL;
        }
    }
}
