using System;
using System.Linq.Expressions;
using mooSQL.data.model;
using mooSQL.linq.translator;

namespace mooSQL.data;

/// <summary>
/// SQLClip 与 Ext LINQ 编译桥接。
/// <para>
/// 将 Ext LINQ 表达式编译为 <see cref="SQLBuilder"/> 并注入 <see cref="SQLClip"/>，
/// 便于在 SQLClip 管线中嵌入已编译的 LINQ 子查询。生产主路径仍是 <c>useQueryable</c>；
/// 本 API 为显式桥接（测试与混合场景使用）。
/// </para>
/// </summary>
public static class SQLClipLinqExtensions
{
    /// <summary>
    /// 将 Ext LINQ 表达式编译为 SQLBuilder 并注入 Clip 上下文。
    /// </summary>
    public static SQLClip FromLinqExpression(this DBInstance db, Expression expression, object?[]? parameters = null)
    {
        var kit = LinqStatementCompiler.ToSQLBuilder(db, expression, parameters);
        return new SQLClip(db, kit);
    }
}
