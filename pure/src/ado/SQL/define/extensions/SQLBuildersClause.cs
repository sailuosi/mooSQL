using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model
{
    /// <summary>
    /// 一组SQLBuilder的持有者，代表复杂SQL模型的构造结果。
    /// </summary>
    public class SQLBuildersClause : Clause
    {
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitSQLBuilders(this);
        }

        public override ClauseType NodeType => ClauseType.SQLBuilder;
        /// <summary>
        /// 来源的语句类型
        /// </summary>
        public ClauseType SrcClauseType { get; set; }


        public List<BuildingSQL> Builders { get; set; }

        /// <summary>
        /// SQL的类型
        /// </summary>
        public SentenceType SQLType { get; set; }

        public SQLBuildersClause() : base(ClauseType.SQLBuilder, null)
        {
            Builders= new List<BuildingSQL>();
        }


    }
}
