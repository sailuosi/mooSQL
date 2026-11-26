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
        /// <summary>
        /// 初始化 ClipConditionVisitor（构造）。
        /// </summary>
        public ClipConditionVisitor(FastCompileContext context, SQLClip clip) : base(context)
        {
            this.clip = clip;
            ValueVisitor = new ValueExpressionVisitor();
            MethodVisitor = new ClipWhereMethodVisitor(this.Context, this);
            ParsedTables = new List<ClipTable>();
        }

        internal List<ClipTable> ParsedTables { get; set; }

        /// <summary>
        /// 最近一次字段解析得到的实体列（供方法型条件变换枚举值）。
        /// </summary>
        internal EntityColumn LastResolvedColumn { get; private set; }

        /// <summary>
        /// 获取字段表达式
        /// </summary>
        /// <param name="node"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        public override string VisitToGotField(Expression node, out MemberInfo member)
        {
            var sql = VisitByClipField(node, out var fidv);
            member = fidv?.member;
            return sql;
        }

        /// <summary>
        /// 解析字段 SQL，并输出列映射信息。
        /// </summary>
        public override string VisitToGotColumn(Expression node, out EntityColumn column)
        {
            var sql = VisitByClipField(node, out var fidv);
            column = ResolveColumnFromVisitor(fidv);
            LastResolvedColumn = column;
            return sql;
        }

        /// <summary>兼容旧 Clip 调用：等同 <see cref="VisitToGotColumn"/>。</summary>
        internal string VisitToGotField(Expression node, out EntityColumn column) =>
            VisitToGotColumn(node, out column);

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

            LastResolvedColumn = ResolveColumnFromVisitor(fidv);

            if (!string.IsNullOrEmpty(sql))
            {
   
                return sql;
            }
            return null;

        }

        static EntityColumn ResolveColumnFromVisitor(ClipFieldVisitor fidv)
        {
            if (fidv == null) return null;
            if (fidv.field != null) return fidv.field;
            if (fidv.ClipFields == null || fidv.ClipFields.Count == 0) return null;
            return fidv.ClipFields[0].EnColumn;
        }

        /// <summary>
        /// 访问ToGotField。
        /// </summary>
        public override string VisitToGotField(Expression node)
        {
            var sql = VisitByClipField(node, out var fidv);
            return sql;
        }

        // VisitBinary 使用基类 ConditionVisitor：TryConvertColumnValue + DataFam 绑参

    }
}
