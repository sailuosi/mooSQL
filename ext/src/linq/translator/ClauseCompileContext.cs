using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using mooSQL.data;
using mooSQL.data.model;
using mooSQL.linq.Linq;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using mooSQL.utils;

namespace mooSQL.linq.translator;

/// <summary>
/// 双工访问器编译阶段的共享状态，封装原 BuildInfo / IBuildContext 栈。
/// </summary>
internal sealed class ClauseCompileContext
{
    public ClauseCompileContext(ClauseSqlTranslator builder, BuildInfo rootBuildInfo)
    {
        Builder = builder;
        Translator = builder;
        RootBuildInfo = rootBuildInfo;
        NavColumns = new Dictionary<Type, List<EntityColumn>>();
    }

    public ClauseSqlTranslator Builder { get; }

    public ClauseSqlTranslator Translator { get; }

    public BuildInfo RootBuildInfo { get; private set; }

    public StatementExpression? StatementResult { get; set; }

    public IBuildContext? BuildContext => StatementResult?.BuildContext;

    public Dictionary<Type, List<EntityColumn>> NavColumns { get; }

    public Type? EntityType { get; set; }

    public BuildInfo CreateBuildInfo(Expression expression)
        => new(RootBuildInfo.Parent, expression, RootBuildInfo.SelectQuery);

    public BuildInfo CreateBuildInfo(Expression expression, BuildInfo from)
        => new(from, expression);

    public void AddNavTarget(Type boss, EntityColumn slave)
    {
        if (!NavColumns.TryGetValue(boss, out var list))
        {
            list = new List<EntityColumn>();
            NavColumns[boss] = list;
        }

        list.AddNotRepeat(slave);
    }

    public SentenceBag<T> ToSentenceBag<T>(StatementExpression statement, Expression? srcExp = null)
    {
        var bag = new SentenceBag<T>
        {
            EntityType = typeof(T),
            buildContext = statement.BuildContext,
            DBLive = Builder.DBLive,
            srcExp = srcExp ?? Builder.Expression
        };

        bag.add(new SentenceItem
        {
            Statement = statement.ToStatement(),
            ParameterAccessors = Builder.ParametersContext.CurrentSqlParameters
        });

        bag.SetParameterized(Builder.ParametersContext.GetParameterized());

        foreach (var pair in NavColumns)
        {
            foreach (var col in pair.Value)
                bag.AddNavColumn(pair.Key, col);
        }

        foreach (var pair in Builder.NavColumns)
        {
            foreach (var col in pair.Value)
                bag.AddNavColumn(pair.Key, col);
        }

        return bag;
    }
}
