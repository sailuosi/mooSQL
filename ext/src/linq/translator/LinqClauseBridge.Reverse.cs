using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using mooSQL.data;
using mooSQL.data.model;
using mooSQL.linq.Linq;

namespace mooSQL.linq.translator;

public static partial class LinqClauseBridge
{
    static readonly ConditionalWeakTable<SQLBuilder, SelectQueryClause> BuilderToSelectQuery = new();

    /// <summary>注册 LINQ 编译产生的 SQLBuilder ↔ SelectQueryClause 映射（供逆向桥接）。</summary>
    internal static void AttachSelectQuery(SQLBuilder builder, SelectQueryClause selectQuery)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (selectQuery == null) throw new ArgumentNullException(nameof(selectQuery));
#if NET451 || NET462
        BuilderToSelectQuery.Remove(builder);
        BuilderToSelectQuery.Add(builder, selectQuery);
#else
        BuilderToSelectQuery.AddOrUpdate(builder, selectQuery);
#endif
    }

    /// <summary>从经 LINQ 桥接产生的 <see cref="SQLBuilder"/> 还原 <see cref="SelectQueryClause"/>。</summary>
    public static SelectQueryClause ToSelectQueryClause(SQLBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (BuilderToSelectQuery.TryGetValue(builder, out var clause))
            return clause;
        throw new InvalidOperationException("SQLBuilder was not produced by Linq compile bridge; no SelectQueryClause attached.");
    }

    /// <summary>从 <see cref="SQLBuilder"/> 重建 SentenceBag（Clause IR 逆向桥接）。</summary>
    internal static SentenceBag FromSQLBuilder(DBInstance db, SQLBuilder builder, Expression? sourceExpression = null)
    {
        if (db == null) throw new ArgumentNullException(nameof(db));
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        var selectQuery = ToSelectQueryClause(builder);
        var bag = new SentenceBag
        {
            DBLive = db,
            srcExp = sourceExpression ?? Expression.Constant(1),
            Sentences = new List<SentenceItem>()
        };
        bag.add(new SentenceItem
        {
            Statement = new SelectSentence(selectQuery)
        });
        return bag;
    }
}
