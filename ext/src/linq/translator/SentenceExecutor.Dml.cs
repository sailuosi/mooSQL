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

    static void EnsureInsertOrUpdateExpanded(SentenceBag bag, DBInstance db)
    {
        if (bag.Sentences.Count != 1)
            return;

        if (bag.Sentences[0].Statement is not InsertOrUpdateSentence)
            return;

        if (db.dialect.Option.ProviderFlags.IsInsertOrUpdateSupported)
            return;

        var firstStatement = (InsertOrUpdateSentence)bag.Sentences[0].Statement;
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

    static RunnerContext CreateContext(SentenceBag bag, DBInstance db, Expression? expression, object?[]? parameters = null, CancellationToken cancellationToken = default)
        => RunnerContextFactory.Create(bag, db, expression, parameters, cancellationToken);

    static (Expression expression, object?[]? parameters) ResolveArgs(RunnerContext context)
        => RunnerContextFactory.ResolveExecutionArgs(context);

    static object? ExecuteWriteOrAlternative(SentenceBag bag, DBInstance db, Expression expression, object?[]? parameters = null)
    {
        EnsureInsertOrUpdateExpanded(bag, db);
        FinalizeBag(bag, db);

        if (TryGetInsertOrUpdateMode(bag, out var mode))
        {
            return mode switch
            {
                InsertOrUpdateExecMode.UpdateThenInsert => ExecuteNonQueryQuery2(CreateContext(bag, db, expression, parameters)),
                InsertOrUpdateExecMode.ExistsThenInsert => ExecuteQueryQuery2(CreateContext(bag, db, expression, parameters)),
                _ => throw new InvalidOperationException()
            };
        }

        if (bag.Sentences.Count == 1 && IsWriteStatement(bag.Sentences[0].Statement))
            return ExecuteModify(CreateContext(bag, db, expression, parameters));

        return null;
    }

        static async Task<object?> ExecuteWriteOrAlternativeAsync(
        SentenceBag bag, DBInstance db, Expression expression, CancellationToken cancellationToken, object?[]? parameters = null)
    {
        EnsureInsertOrUpdateExpanded(bag, db);
        FinalizeBag(bag, db);

        if (TryGetInsertOrUpdateMode(bag, out var mode))
        {
            var context = CreateContext(bag, db, expression, parameters, cancellationToken);
            return mode switch
            {
                InsertOrUpdateExecMode.UpdateThenInsert => await ExecuteNonQueryQuery2Async(context),
                InsertOrUpdateExecMode.ExistsThenInsert => await ExecuteQueryQuery2Async(context),
                _ => throw new InvalidOperationException()
            };
        }

        if (bag.Sentences.Count == 1 && IsWriteStatement(bag.Sentences[0].Statement))
            return await ExecuteModifyAsync(CreateContext(bag, db, expression, parameters, cancellationToken));

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
        if (cmds.Count < 2)
            return context.dataContext.ExeNonQuery(cmds[0]);

        using var tran = context.dataContext.beginTransaction();
        try
        {
            var n = tran.SetSQL(cmds[0]).Executing((executor, ctx) => executor.ExecuteNonQuery(ctx));
            if (n != 0)
            {
                tran.CommitOrRollback();
                return n;
            }

            var inserted = tran.SetSQL(cmds[1]).Executing((executor, ctx) => executor.ExecuteNonQuery(ctx));
            tran.CommitOrRollback();
            return inserted;
        }
        catch
        {
            tran.CommitOrRollback();
            throw;
        }
    }

    static async Task<object?> ExecuteNonQueryQuery2Async(RunnerContext context)
    {
        using var _ = ActivityService.Start(ActivityID.ExecuteNonQuery2Async);
        var cmds = PrepareCommands(context);
        if (cmds.Count < 2)
            return await context.dataContext.ExeNonQueryAsync(cmds[0], context.cancellationToken).ConfigureAwait(false);

        using var tran = context.dataContext.beginTransaction();
        try
        {
            var n = tran.SetSQL(cmds[0]).Executing((executor, ctx) => executor.ExecuteNonQuery(ctx));
            if (n != 0)
            {
                tran.CommitOrRollback();
                return n;
            }

            var inserted = tran.SetSQL(cmds[1]).Executing((executor, ctx) => executor.ExecuteNonQuery(ctx));
            tran.CommitOrRollback();
            return inserted;
        }
        catch
        {
            tran.CommitOrRollback();
            throw;
        }
    }

    static int ExecuteQueryQuery2(RunnerContext context)
    {
        using var _ = ActivityService.Start(ActivityID.ExecuteScalarAlternative);
        var cmds = PrepareCommands(context);
        if (cmds.Count < 2)
        {
            var exists = context.dataContext.ExeQueryScalar(cmds[0]);
            return exists != null ? 0 : context.dataContext.ExeNonQuery(cmds[0]);
        }

        using var tran = context.dataContext.beginTransaction();
        try
        {
            var exists = tran.SetSQL(cmds[0]).Executing((executor, ctx) => executor.ExecuteScalar(ctx));
            if (exists != null)
            {
                tran.CommitOrRollback();
                return 0;
            }

            var inserted = tran.SetSQL(cmds[1]).Executing((executor, ctx) => executor.ExecuteNonQuery(ctx));
            tran.CommitOrRollback();
            return inserted;
        }
        catch
        {
            tran.CommitOrRollback();
            throw;
        }
    }

    static async Task<object?> ExecuteQueryQuery2Async(RunnerContext context)
    {
        var cmds = PrepareCommands(context);
        if (cmds.Count < 2)
        {
            var scalar = await context.dataContext.ExeQueryScalarAsync(cmds[0], context.cancellationToken).ConfigureAwait(false);
            return scalar != null ? 0 : await context.dataContext.ExeNonQueryAsync(cmds[0], context.cancellationToken).ConfigureAwait(false);
        }

        return await Task.Run(() => (object?)ExecuteQueryQuery2(context)).ConfigureAwait(false);
    }

    /// <summary>
    /// 多语句 DML 在同一事务中顺序执行（InsertOrUpdate 展开等场景）。
    /// </summary>
    internal static int ExecuteWriteBatchInTransaction(RunnerContext context, IReadOnlyList<SQLCmd> cmds)
    {
        if (cmds.Count == 0)
            return 0;

        if (cmds.Count == 1)
            return context.dataContext.ExeNonQuery(cmds[0]);

        using var tran = context.dataContext.beginTransaction();
        try
        {
            var total = 0;
            foreach (var cmd in cmds)
                total += tran.SetSQL(cmd).Executing((executor, ctx) => executor.ExecuteNonQuery(ctx));
            tran.CommitOrRollback();
            return total;
        }
        catch
        {
            tran.CommitOrRollback();
            throw;
        }
    }
}
