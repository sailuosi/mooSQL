using System;
using System.Linq.Expressions;
using System.Reflection;

namespace mooSQL.data.translation
{
    /// <summary>从 Lambda 提取 MethodInfo / PropertyInfo（注册 Translation 用）。</summary>
    public static class ExpressionMemberHelper
    {
        public static MemberInfo GetMemberInfo(LambdaExpression func) => GetMemberInfo(func.Body);

        public static MemberInfo GetMemberInfo(Expression expr)
        {
            return expr.NodeType switch
            {
                ExpressionType.Call       => ((MethodCallExpression)expr).Method,
                ExpressionType.MemberAccess => ((MemberExpression)expr).Member,
                ExpressionType.New        => ((NewExpression)expr).Constructor!,
                ExpressionType.Convert or ExpressionType.ConvertChecked
                                          => GetMemberInfo(((UnaryExpression)expr).Operand),
                _ => throw new ArgumentException(
                    $"Unsupported expression type '{expr.NodeType}' for member registration.", nameof(expr))
            };
        }
    }
}
