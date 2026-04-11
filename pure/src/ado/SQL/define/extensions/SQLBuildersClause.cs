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
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitSQLBuilders(this);
        }

        /// <inheritdoc />
        public override ClauseType NodeType => ClauseType.SQLBuilder;
        /// <summary>
        /// 来源的语句类型
        /// </summary>
        public ClauseType SrcClauseType { get; set; }


        /// <summary>并行构建的多个 <see cref="BuildingSQL"/> 片段。</summary>
        public List<BuildingSQL> Builders { get; set; }

        /// <summary>
        /// SQL的类型
        /// </summary>
        public SentenceType SQLType { get; set; }

        /// <summary>初始化空构建器列表。</summary>
        public SQLBuildersClause() : base(ClauseType.SQLBuilder, null)
        {
            Builders= new List<BuildingSQL>();
        }


    }
}
