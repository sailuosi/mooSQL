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
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitSQLFrag(this);
        }

        /// <summary>原始 SQL 文本。</summary>
        public string SQL;

        /// <summary>关联参数集合（若有）。</summary>
        public Paras Para;
        /// <summary>仅文本片段。</summary>
        public SQLFragClause(string SQL) : base(ClauseType.SQLFrag, null)
        {
            this.SQL = SQL;
        }
        /// <summary>文本与参数。</summary>
        public SQLFragClause(string SQL, Paras Para) : base(ClauseType.SQLFrag, null)
        { 
            this.Para = Para;
            this.SQL = SQL;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return this.SQL;
        }
    }
}
