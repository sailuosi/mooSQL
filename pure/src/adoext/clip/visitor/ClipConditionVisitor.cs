using mooSQL.linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.clip
{

    /// <summary>
    /// where子句构建
    /// </summary>
    public class ClipConditionVisitor : ConditionVisitor
    {
        private SQLClip clip;
        public ClipConditionVisitor(FastCompileContext context, SQLClip clip) : base(context)
        {
            this.clip = clip;
            ValueVisitor = new ValueExpressionVisitor();
            MethodVisitor = new WhereMethodVisitor(this.Context, this);
            ParsedTables = new List<ClipTable>();
        }

        internal List<ClipTable> ParsedTables { get; set; }
        /// <summary>
        /// 获取字段表达式
        /// </summary>
        /// <param name="node"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        public override string VisitToGotField(Expression node, out MemberInfo member)
        {
            var sql = VisitByClipField(node, out var fidv);
            member = fidv.member;
            return sql;
        }

        private string VisitByClipField(Expression node, out ClipFieldVisitor fidv) {
            bool needNick = true;
            if (Context.RunType == LayerRunType.update && Builder.FromCount == 0)
            {
                needNick = false;
            }
            fidv = new ClipFieldVisitor(Context, needNick, clip);
            var fie = fidv.Visit(node);
            var sql = fidv.GetFieldCondtionSQL();

            if (fidv.ClipFields != null) { 
                foreach (var item in fidv.ClipFields)
                {
                    this.ParsedTables.Add(item.CTable);
                }
            }
            if (fidv.table != null)
            {
                this.ParsedTables.Add(fidv.table);
            }
            if (!string.IsNullOrEmpty(sql))
            {
   
                return sql;
            }
            return null;

        }

        public override string VisitToGotField(Expression node)
        {
            var sql = VisitByClipField(node, out var fidv);
            return sql;
        }


    }
}
