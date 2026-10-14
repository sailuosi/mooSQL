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
        internal string VisitToGotField(Expression node, out EntityColumn column)
        {
            var sql = VisitByClipField(node, out var fidv);
            column = ResolveColumnFromVisitor(fidv);
            LastResolvedColumn = column;
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

        /// <summary>
        /// Clip 专用比较：按左侧列信息变换右侧枚举值后再 where（不影响 SQLBuilder / 基类路径）。
        /// </summary>
        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.And || node.NodeType == ExpressionType.AndAlso)
            {
                Builder.sink();
                Visit(node.Left);
                Visit(node.Right);
                Builder.rise();
                return node;
            }
            else if (node.NodeType == ExpressionType.Or || node.NodeType == ExpressionType.OrElse)
            {
                Builder.sinkOR();
                Visit(node.Left);
                Visit(node.Right);
                Builder.rise();
                return node;
            }

            var op = this.GetCompareOp(node.NodeType);
            if (op != null)
            {
                EntityColumn leftCol = null;
                var fie = VisitToGotField(node.Left, out leftCol);
                WhereFrag wh = new WhereFrag();
                wh.op = op;
                bool fail = false;
                if (fie != null)
                {
                    wh.key = fie;
                }
                else
                {
                    var leftVal = VisitToGotValue(node.Left);
                    if (leftVal != null)
                    {
                        wh.leftValue = EnumColumnMapping.ResolveClipValue(leftVal, leftCol);
                    }
                    else
                    {
                        fail = true;
                    }
                }

                if (fail == false)
                {
                    var valuesql = VisitToGotField(node.Right);
                    if (valuesql != null)
                    {
                        wh.value = valuesql;
                        wh.paramed = false;
                        Builder.where(wh);
                    }
                    else
                    {
                        var constant = VisitToGotValue(node.Right);
                        if (constant != null)
                        {
                            wh.value = EnumColumnMapping.ResolveClipValue(constant, leftCol);
                            wh.paramed = true;
                            Builder.where(wh);
                        }
                    }
                }
            }

            return node;
        }


    }
}
