using System;
using System.Linq.Expressions;
using mooSQL.data;
using mooSQL.data.model;
using mooSQL.linq.translator;

namespace mooSQL.data;

/// <summary>SQLClip 与 Ext LINQ 编译桥接。</summary>
public static class SQLClipLinqExtensions
{
    /// <summary>将 Ext LINQ 表达式编译为 SQLBuilder 并注入 Clip 上下文。</summary>
    public static SQLClip FromLinqExpression(this DBInstance db, Expression expression, object?[]? parameters = null)
    {
        var kit = LinqStatementCompiler.ToSQLBuilder(db, expression, parameters);
        return new SQLClip(db, kit);
    }
}
