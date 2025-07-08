using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model
{
    /// <summary>
    /// SQLBuilder的持有者
    /// </summary>
    public class SQLBuilderClause:Clause
    {
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitSQLBuilder(this);
        }

        public override ClauseType NodeType => ClauseType.SQLBuilder;
        /// <summary>
        /// 来源的语句类型
        /// </summary>
        public BuildingSQL TargetSQL { get; set; }


        public SQLBuilder Builder {
            get { 
                return TargetSQL.Builder;
            }
        }
        /// <summary>
        /// SQL的类型
        /// </summary>
        public SentenceType SQLType { get; set; }

        public SQLBuilderClause(SQLBuilder builder) : base(ClauseType.SQLBuilder, null)
        {
            TargetSQL = new BuildingSQL();
            TargetSQL. Builder = builder;
        }

        public SQLBuilderClause(SQLBuilder builder, Func<BuildingSQL, SQLCmd> toCmd) : base(ClauseType.SQLBuilder, null)
        {
            TargetSQL = new BuildingSQL();
            TargetSQL.Builder = builder;
            TargetSQL.ToCmd = toCmd;
        }

        public SQLCmd ToCmd() {
            return TargetSQL.ToCmd(TargetSQL);
        }
    }
}
