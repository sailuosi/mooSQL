using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using mooSQL.data;
using mooSQL.data.model;
using mooSQL.linq.Linq;

namespace mooSQL.linq.translator;

internal static class SqlPlanBuilder
{
    public static SqlPlan Build(
        SentenceBag bag,
        DBInstance db,
        Expression expression,
        StatementCompileOptions options,
        IReadOnlyList<string> stages)
    {
        var items = new List<StatementPlanItem>();

        if (options.FinalizeBeforeStructureRead
            && bag.ErrorExpression == null
            && bag.Sentences is { Count: > 0 })
        {
            SentenceExecutor.FinalizeBag(bag, db);
        }

        if (bag.Sentences != null)
        {
            foreach (var sentence in bag.Sentences)
            {
                var parameterValues = new SqlParameterValues();
                QueryMate.SetParameters(bag, expression, db, null, sentence, parameterValues);

                items.Add(new StatementPlanItem
                {
                    Structure       = StatementStructureReader.Read(sentence.Statement, parameterValues),
                    DebugTree       = sentence.Statement.SelectQuery?.SqlText,
                    ParameterCount  = sentence.ParameterAccessors?.Count ?? 0
                });
            }
        }

        string? sqlPreview = null;
        if (options.IncludeFinalizedSql
            && bag.ErrorExpression == null
            && bag.Sentences is { Count: > 0 })
        {
            sqlPreview = SentenceExecutor.GetSqlText(bag, db, expression);
            stages = stages.Concat(["Statement.Finalize"]).ToList();
        }

        return new SqlPlan
        {
            EntityTypeName = bag.EntityType?.Name,
            HasError       = bag.ErrorExpression != null,
            Stages         = stages,
            Statements     = items,
            SqlPreview     = sqlPreview,
            NavColumnCount = bag.NavColumns.Count,
            IsCacheable    = bag.IsCacheable
        };
    }
}
