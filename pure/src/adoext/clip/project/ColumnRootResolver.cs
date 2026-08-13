using System;
using System.Linq.Expressions;
using System.Reflection;
using mooSQL.linq;
using mooSQL.utils;

namespace mooSQL.data.clip.project
{
    /// <summary>
    /// 解析「表变量.列」列根；供廉价探测与尾投影分析共用。
    /// </summary>
    internal static class ColumnRootResolver
    {
        public static Expression UnwrapConvert(Expression expr)
        {
            while (expr is UnaryExpression u &&
                   (u.NodeType == ExpressionType.Convert || u.NodeType == ExpressionType.ConvertChecked ||
                    u.NodeType == ExpressionType.TypeAs))
            {
                expr = u.Operand;
            }
            return expr;
        }

        public static bool TryResolve(SQLClip clip, Expression expression, out ColumnRoot root)
        {
            root = null;
            var expr = UnwrapConvert(expression);
            if (expr is not MemberExpression me)
                return false;

            if (me.Member is not PropertyInfo && me.Member is not FieldInfo)
                return false;

            // 列访问形态：closure.tableVar.Column 或 tableConst.Column
            var findConst = new ExpressionFindVisitor<ConstantExpression>();
            var constExpr = findConst.Find(me.Expression);
            var findCaller = new ExpressionFindVisitor<MemberExpression>();
            var callerExpr = findCaller.Find(me.Expression);
            if (constExpr == null || callerExpr == null)
                return false;

            var callerName = callerExpr.Member.Name;
            var tb = FindTable(clip, constExpr, callerName);
            if (tb?.TableInfo == null)
                return false;

            var col = tb.TableInfo.GetColumn(me.Member.Name);
            if (col == null)
                return false;

            if (string.IsNullOrWhiteSpace(tb.Alias))
                tb.Alias = callerName;

            root = new ColumnRoot
            {
                Expression = me,
                Table = tb,
                Column = col,
                Alias = tb.Alias,
                SqlField = col.DbColumnName,
                ClrType = col.PropertyInfo?.PropertyType ?? me.Type,
                Key = (tb.Alias ?? "") + "\0" + (col.DbColumnName ?? me.Member.Name),
            };
            return true;
        }

        private static ClipTable FindTable(SQLClip clip, ConstantExpression exp, string name)
        {
            if (ClosureInspector.IsClosureClass(exp.Type))
            {
                var v = ClosureInspector.GetFieldValueN(exp.Value, name);
                if (v != null && clip.Context.BindTables.TryGetValue(v, out var tb0))
                    return tb0;
            }

            if (clip.Context.BindTables.TryGetValue(exp.Value, out var tb))
                return tb;
            return null;
        }
    }

    internal sealed class ColumnRoot
    {
        public MemberExpression Expression { get; set; }
        public ClipTable Table { get; set; }
        public EntityColumn Column { get; set; }
        public string Alias { get; set; }
        public string SqlField { get; set; }
        public Type ClrType { get; set; }
        public string Key { get; set; }
    }
}
