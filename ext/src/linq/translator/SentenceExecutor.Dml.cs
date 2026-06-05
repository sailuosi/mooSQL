using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using mooSQL.data;
using mooSQL.data.model;
using mooSQL.linq.Linq;
using mooSQL.linq.SqlQuery;
using mooSQL.linq.Tools;

namespace mooSQL.linq.translator;

internal static partial class SentenceExecutor
{
    enum InsertOrUpdateExecMode
    {
        UpdateThenInsert,
        ExistsThenInsert,
    }

    static bool IsWriteStatement(BaseSentence statement)
    {
        return statement.QueryType is QueryType.Insert
            or QueryType.Update
            or QueryType.Delete
            or QueryType.Merge
            or QueryType.InsertOrUpdate
            or QueryType.MultiInsert
            or QueryType.TruncateTable;
    }

    static void EnsureInsertOrUpdateExpanded(SentenceBag bag)
    {
        if (bag.Sentences.Count != 1)
            return;

        if (bag.Sentences[0].Statement is not InsertOrUpdateSentence firstStatement)
            return;

        var cloned = firstStatement.Clone();
        var insertStatement = new InsertSentence(cloned.SelectQuery)
        {
            Insert = cloned.Insert,
            Tag = cloned.Tag,
            SqlQueryExtensions = cloned.SqlQueryExtensions
        };

        insertStatement.SelectQuery.From.Tables.Clear();

        bag.Sentences.Add(new SentenceItem
        {
            Statement = insertStatement,
            ParameterAccessors = bag.Sentences[0].ParameterAccessors
        });

        var keys = firstStatement.Update.Keys;
        var wsc = firstStatement.SelectQuery.Where.EnsureConjunction();

        foreach (var key in keys)
            wsc.AddEqual(key.Column, key.Expression!, false);

        if (firstStatement.Update.Items.Count > 0)
        {
            bag.Sentences[0].Statement = new UpdateSentence(firstStatement.SelectQuery)
            {
                Update = firstStatement.Update,
                Tag = firstStatement.Tag,
                SqlQueryExtensions = firstStatement.SqlQueryExtensions
            };
        }
        else
        {
            firstStatement.SelectQuery.Select.Columns.Clear();
            firstStatement.SelectQuery.Select.Columns.Add(new ColumnWord(firstStatement.SelectQuery, new ExpressionWord("1")));
            bag.Sentences[0].Statement = new SelectSentence(firstStatement.SelectQuery);
        }

        bag.Sentences.Add(new SentenceItem
        {
            Statement = new SelectSentence(firstStatement.SelectQuery),
            ParameterAccessors = bag.Sentences[0].ParameterAccessors.ToList(),
        });

        bag.IsFinalized = false;
    }

    static bool TryGetInsertOrUpdateMode(SentenceBag bag, out InsertOrUpdateExecMode mode)
    {
        mode = default;

        if (bag.Sentences.Count < 2)
            return false;

        if (bag.Sentences[0].Statement is UpdateSentence
            && bag.Sentences[1].Statement is InsertSentence)
        {
            mode = InsertOrUpdateExecMode.UpdateThenInsert;
            return true;
        }

        if (bag.Sentences[0].Statement is SelectSentence
            && bag.Sentences[1].Statement is InsertSentence)
        {
            mode = InsertOrUpdateExecMode.ExistsThenInsert;
            return true;
        }

        return false;
    }

    static List<SQLCmd> PrepareCommands(RunnerContext context)
    {
        var bag = context.sentenceBag ?? throw new InvalidOperationException("RunnerContext.sentenceBag is required.");

        var res = new List<SQLCmd>();
        foreach (var sentence in bag.Sentences)
        {
            var translated = sentence.cmds ?? QueryMate.TranslateCmds(context, sentence, false);
            foreach (var cmd in translated.cmds)
                res.Add(cmd);
        }

        return res;
    }

    static RunnerContext CreateContext(SentenceBag bag, DBInstance db, Expression expression, CancellationToken cancellationToken = default)
        => new()
        {
            sentenceBag = bag,
            dataContext = db,
            expression = expression,
            paras = null,
            cancellationToken = cancellationToken
        };

    static object? ExecuteWriteOrAlternative(SentenceBag bag, DBInstance db, Expression expression)
    {
        EnsureInsertOrUpdateExpanded(bag);
        FinalizeBag(bag, db);

        if (TryGetInsertOrUpdateMode(bag, out var mode))
        {
            return mode switch
            {
                InsertOrUpdateExecMode.UpdateThenInsert => ExecuteNonQueryQuery2(CreateContext(bag, db, expression)),
                InsertOrUpdateExecMode.ExistsThenInsert => ExecuteQueryQuery2(CreateContext(bag, db, expression)),
                _ => throw new InvalidOperationException()
            };
        }

        if (bag.Sentences.Count == 1 && IsWriteStatement(bag.Sentences[0].Statement))
            return ExecuteModify(CreateContext(bag, db, expression));

        return null;
    }

    static async Task<object?> ExecuteWriteOrAlternativeAsync(
        SentenceBag bag, DBInstance db, Expression expression, CancellationToken cancellationToken)
    {
        EnsureInsertOrUpdateExpanded(bag);
        FinalizeBag(bag, db);

        if (TryGetInsertOrUpdateMode(bag, out var mode))
        {
            var context = CreateContext(bag, db, expression, cancellationToken);
            return mode switch
            {
                InsertOrUpdateExecMode.UpdateThenInsert => await ExecuteNonQueryQuery2Async(context),
                InsertOrUpdateExecMode.ExistsThenInsert => await ExecuteQueryQuery2Async(context),
                _ => throw new InvalidOperationException()
            };
        }

        if (bag.Sentences.Count == 1 && IsWriteStatement(bag.Sentences[0].Statement))
            return await ExecuteModifyAsync(CreateContext(bag, db, expression, cancellationToken));

        return null;
    }

    static int ExecuteModify(RunnerContext context)
    {
        using var _ = ActivityService.Start(ActivityID.ExecuteNonQuery);
        var cmds = PrepareCommands(context);
        return context.dataContext.ExeNonQuery(cmds[0]);
    }

    static Task<int> ExecuteModifyAsync(RunnerContext context)
    {
        using var _ = ActivityService.Start(ActivityID.ExecuteNonQueryAsync);
        var cmds = PrepareCommands(context);
        return context.dataContext.ExeNonQueryAsync(cmds[0], context.cancellationToken);
    }

    static int ExecuteNonQueryQuery2(RunnerContext context)
    {
        using var _ = ActivityService.Start(ActivityID.ExecuteNonQuery2);
        var cmds = PrepareCommands(context);
        var n = context.dataContext.ExeNonQuery(cmds[0]);
        if (n != 0)
            return n;

        return context.dataContext.ExeNonQuery(cmds[1]);
    }

    static async Task<object?> ExecuteNonQueryQuery2Async(RunnerContext context)
    {
        using var _ = ActivityService.Start(ActivityID.ExecuteNonQuery2Async);
        var cmds = PrepareCommands(context);
        var n = await context.dataContext.ExeNonQueryAsync(cmds[0], context.cancellationToken);
        if (n != 0)
            return n;

        return await context.dataContext.ExeNonQueryAsync(cmds[1], context.cancellationToken);
    }

    static int ExecuteQueryQuery2(RunnerContext context)
    {
        using var _ = ActivityService.Start(ActivityID.ExecuteScalarAlternative);
        var cmds = PrepareCommands(context);
        var n = context.dataContext.ExeQueryScalar(cmds[0]);
        if (n != null)
            return 0;

        return context.dataContext.ExeNonQuery(cmds[1]);
    }

    static async Task<object?> ExecuteQueryQuery2Async(RunnerContext context)
    {
        var cmds = PrepareCommands(context);
        var n = await context.dataContext.ExeQueryScalarAsync(cmds[0], context.cancellationToken);
        if (n != null)
            return 0;

        return await context.dataContext.ExeNonQueryAsync(cmds[1], context.cancellationToken);
    }
}
