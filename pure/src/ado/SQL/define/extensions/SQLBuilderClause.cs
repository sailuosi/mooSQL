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
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitSQLBuilder(this);
        }

        /// <inheritdoc />
        public override ClauseType NodeType => ClauseType.SQLBuilder;
        /// <summary>
        /// 来源的语句类型
        /// </summary>
        public BuildingSQL TargetSQL { get; set; }


        /// <summary>当前嵌入的 <see cref="SQLBuilder"/>（来自 <see cref="TargetSQL"/>）。</summary>
        public SQLBuilder Builder {
            get { 
                return TargetSQL.Builder;
            }
        }
        /// <summary>
        /// SQL的类型
        /// </summary>
        public SentenceType SQLType { get; set; }

        /// <summary>仅包装 <see cref="SQLBuilder"/>。</summary>
        public SQLBuilderClause(SQLBuilder builder) : base(ClauseType.SQLBuilder, null)
        {
            TargetSQL = new BuildingSQL();
            TargetSQL. Builder = builder;
        }

        /// <summary>包装 <see cref="SQLBuilder"/> 并指定自定义 <see cref="BuildingSQL.ToCmd"/>。</summary>
        public SQLBuilderClause(SQLBuilder builder, Func<BuildingSQL, SQLCmd> toCmd) : base(ClauseType.SQLBuilder, null)
        {
            TargetSQL = new BuildingSQL();
            TargetSQL.Builder = builder;
            TargetSQL.ToCmd = toCmd;
        }

        /// <summary>调用 <see cref="BuildingSQL.ToCmd"/> 生成命令。</summary>
        public SQLCmd ToCmd() {
            return TargetSQL.ToCmd(TargetSQL);
        }
    }
}
