using System.Linq.Expressions;
using mooSQL.data.model;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;

namespace mooSQL.linq.translator;

/// <summary>
/// Where/Having lambda 内字段路径解析（Phase E）。
/// </summary>
internal static class ClauseFieldVisitor
{
    public static IExpWord? ConvertField(ClauseSqlTranslator builder, IBuildContext context, Expression expr, ProjectFlags flags)
    {
        if (expr == null)
            return null;

        return builder.ConvertToSql(context, expr.Unwrap(), unwrap: false, flags: flags);
    }
}
