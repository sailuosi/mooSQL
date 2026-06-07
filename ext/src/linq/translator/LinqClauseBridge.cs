using System;
using System.Linq.Expressions;
using mooSQL.data;
using mooSQL.data.model;
using mooSQL.linq.translator;

namespace mooSQL.linq.translator;

/// <summary>SentenceBag / SelectQueryClause ↔ SQLBuilder 桥接。</summary>
public static partial class LinqClauseBridge
{
    /// <summary>从编译结果获取可执行的 <see cref="SQLBuilder"/>。</summary>
    public static SQLBuilder ToSQLBuilder(this StatementCompileResult result, DBInstance db, object?[]? parameters = null)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        if (db == null) throw new ArgumentNullException(nameof(db));
        return LinqStatementCompiler.ToSQLBuilder(db, result.Expression, parameters);
    }

    /// <summary>
    /// 从首条 SELECT 的 <see cref="SelectQueryClause"/> 创建新的 SQLBuilder（Clause IR 快照桥接）。
    /// </summary>
    public static SQLBuilder FromPrimarySelectQuery(DBInstance db, SelectQueryClause selectQuery)
    {
        if (db == null) throw new ArgumentNullException(nameof(db));
        if (selectQuery == null) throw new ArgumentNullException(nameof(selectQuery));

        var kit = db.useSQL();
        var clause = new SelectSentence(selectQuery);
        var translated = db.dialect.clauseTranslator.Prepare(db).Visit(clause);
        if (translated is SQLBuilderClause builderClause)
        {
            AttachSelectQuery(builderClause.Builder, selectQuery);
            return builderClause.Builder;
        }

        throw new InvalidOperationException("Expected SQLBuilderClause from SelectQueryClause translation.");
    }
}
