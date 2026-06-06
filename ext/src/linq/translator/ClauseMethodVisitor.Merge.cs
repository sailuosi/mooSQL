using mooSQL.data.call;
using mooSQL.data.model;
using mooSQL.linq;
using mooSQL.linq.Data;
using mooSQL.linq.Linq;
using mooSQL.linq.Expressions;
using mooSQL.linq.ext;
using mooSQL.linq.Extensions;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.Reflection;
using mooSQL.linq.SqlQuery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using static mooSQL.linq.Reflection.Methods.SooQuery.Merge;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    static readonly MethodInfo[] MergeExecuteMethods =
    {
        ExecuteMergeMethodInfo,
        MergeWithOutput,
        MergeWithOutputSource,
        MergeWithOutputInto,
        MergeWithOutputIntoSource
    };

    static readonly MethodInfo[] MergeStartMethods = { MergeMethodInfo1, MergeMethodInfo2 };

    static readonly MethodInfo[] MergeIntoMethods = { MergeIntoMethodInfo1, MergeIntoMethodInfo2 };

    static readonly MethodInfo[] OnMethods = { OnMethodInfo1, OnMethodInfo2, OnTargetKeyMethodInfo };

    static readonly MethodInfo[] UsingMethods = { UsingMethodInfo1, UsingMethodInfo2 };

    public override MethodCall VisitMerge(MergeCall method) => VisitMergeClause(method, CanBuildMerge);
    public override MethodCall VisitMergeInto(MergeIntoCall method) => VisitMergeClause(method, CanBuildMergeInto);
    public override MethodCall VisitMergeWithOutput(MergeWithOutputCall method) => VisitMergeClause(method, CanBuildMergeExecute);
    public override MethodCall VisitMergeWithOutputInto(MergeWithOutputIntoCall method) => VisitMergeClause(method, CanBuildMergeExecute);
    public override MethodCall VisitOn(OnCall method) => VisitMergeClause(method, CanBuildOn);
    public override MethodCall VisitOnTargetKey(OnTargetKeyCall method) => VisitMergeClause(method, CanBuildOn);
    public override MethodCall VisitUsing(UsingCall method) => VisitMergeClause(method, CanBuildUsing);
    public override MethodCall VisitUsingTarget(UsingTargetCall method) => VisitMergeClause(method, CanBuildUsingTarget);
    public override MethodCall VisitInsertWhenNotMatchedAnd(InsertWhenNotMatchedAndCall method) => VisitMergeClause(method, CanBuildInsertWhenNotMatched);
    public override MethodCall VisitDeleteWhenMatchedAnd(DeleteWhenMatchedAndCall method) => VisitMergeClause(method, CanBuildDeleteWhenMatched);
    public override MethodCall VisitDeleteWhenNotMatchedBySourceAnd(DeleteWhenNotMatchedBySourceAndCall method) => VisitMergeClause(method, CanBuildDeleteWhenNotMatchedBySource);
    public override MethodCall VisitUpdateWhenMatchedAnd(UpdateWhenMatchedAndCall method) => VisitMergeClause(method, CanBuildUpdateWhenMatched);
    public override MethodCall VisitUpdateWhenMatchedAndThenDelete(UpdateWhenMatchedAndThenDeleteCall method) => VisitMergeClause(method, CanBuildUpdateWhenMatchedThenDelete);
    public override MethodCall VisitUpdateWhenNotMatchedBySourceAnd(UpdateWhenNotMatchedBySourceAndCall method) => VisitMergeClause(method, CanBuildUpdateWhenNotMatchedBySource);

    MethodCall VisitMergeClause(MethodCall method, Func<MethodCallExpression, bool> canBuild)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!canBuild(methodCall))
            return method;

        var buildContext = BuildMergeClause(Context.Builder, buildInfo, methodCall);
        if (buildContext == null)
            return method;

        return ToStatementCallOr(method, buildContext);
    }

    static IBuildContext? BuildMergeClause(ClauseSqlTranslator builder, BuildInfo buildInfo, MethodCallExpression methodCall)
    {
        if (CanBuildMergeStart(methodCall))
            return BuildMergeStart(builder, buildInfo, methodCall);

        if (CanBuildMergeExecute(methodCall))
            return BuildMergeExecute(builder, buildInfo, methodCall);

        if (CanBuildMergeInto(methodCall))
            return BuildMergeInto(builder, buildInfo, methodCall);

        if (CanBuildOn(methodCall))
            return BuildOn(builder, buildInfo, methodCall);

        if (CanBuildUsing(methodCall))
            return BuildUsing(builder, buildInfo, methodCall);

        if (CanBuildUsingTarget(methodCall))
            return BuildUsingTarget(builder, buildInfo, methodCall);

        if (CanBuildInsertWhenNotMatched(methodCall))
            return BuildInsertWhenNotMatched(builder, buildInfo, methodCall);

        if (CanBuildDeleteWhenMatched(methodCall))
            return BuildDeleteWhenMatched(builder, buildInfo, methodCall);

        if (CanBuildDeleteWhenNotMatchedBySource(methodCall))
            return BuildDeleteWhenNotMatchedBySource(builder, buildInfo, methodCall);

        if (CanBuildUpdateWhenMatched(methodCall))
            return BuildUpdateWhenMatched(builder, buildInfo, methodCall);

        if (CanBuildUpdateWhenMatchedThenDelete(methodCall))
            return BuildUpdateWhenMatchedThenDelete(builder, buildInfo, methodCall);

        if (CanBuildUpdateWhenNotMatchedBySource(methodCall))
            return BuildUpdateWhenNotMatchedBySource(builder, buildInfo, methodCall);

        return null;
    }

    static bool CanBuildMerge(MethodCallExpression call)
        => CanBuildMergeStart(call) || CanBuildMergeExecute(call);

    static bool CanBuildMergeStart(MethodCallExpression call)
        => call.IsSameGenericMethod(MergeStartMethods);

    static bool CanBuildMergeExecute(MethodCallExpression call)
        => call.IsSameGenericMethod(MergeExecuteMethods);

    static bool CanBuildMergeInto(MethodCallExpression call)
        => call.IsSameGenericMethod(MergeIntoMethods);

    static bool CanBuildOn(MethodCallExpression call)
        => call.IsSameGenericMethod(OnMethods);

    static bool CanBuildUsing(MethodCallExpression call)
        => call.IsSameGenericMethod(UsingMethods);

    static bool CanBuildUsingTarget(MethodCallExpression call)
        => call.IsSameGenericMethod(UsingTargetMethodInfo);

    static bool CanBuildInsertWhenNotMatched(MethodCallExpression call)
        => call.IsSameGenericMethod(InsertWhenNotMatchedAndMethodInfo);

    static bool CanBuildDeleteWhenMatched(MethodCallExpression call)
        => call.IsSameGenericMethod(DeleteWhenMatchedAndMethodInfo);

    static bool CanBuildDeleteWhenNotMatchedBySource(MethodCallExpression call)
        => call.IsSameGenericMethod(DeleteWhenNotMatchedBySourceAndMethodInfo);

    static bool CanBuildUpdateWhenMatched(MethodCallExpression call)
        => call.IsSameGenericMethod(UpdateWhenMatchedAndMethodInfo);

    static bool CanBuildUpdateWhenMatchedThenDelete(MethodCallExpression call)
        => call.IsSameGenericMethod(UpdateWhenMatchedAndThenDeleteMethodInfo);

    static bool CanBuildUpdateWhenNotMatchedBySource(MethodCallExpression call)
        => call.IsSameGenericMethod(UpdateWhenNotMatchedBySourceAndMethodInfo);

    static IBuildContext BuildMergeStart(ClauseSqlTranslator builder, BuildInfo buildInfo, MethodCallExpression methodCall)
    {
        var disableFilters = methodCall.Arguments[0] is not MethodCallExpression mc || mc.Method.Name != nameof(LinqExtensions.AsCte);
        if (disableFilters)
            builder.PushDisabledQueryFilters(new Type[] { });

        var target = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], new SelectQueryClause()) { AssociationsAsSubQueries = true });

        if (disableFilters)
            builder.PopDisabledFilter();

        var targetTable = MergeContext.GetTargetTable(target);

        if (targetTable == null)
            throw new NotImplementedException("Currently, only CTEs are supported as the target of a merge. You can fix by calling .AsCte() before calling .Merge()");

        var merge = new MergeSentence(targetTable);
        if (methodCall.Arguments.Count == 2)
            merge.Hint = builder.EvaluateExpression<string>(methodCall.Arguments[1]);

        target.SetAlias(merge.Target.FindAlias()!);

        return new MergeContext(merge, target);
    }

    static IBuildContext BuildMergeExecute(ClauseSqlTranslator builder, BuildInfo buildInfo, MethodCallExpression methodCall)
    {
        var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

        var kind =
            methodCall.IsSameGenericMethod(MergeWithOutput)           ? MergeKind.MergeWithOutput :
            methodCall.IsSameGenericMethod(MergeWithOutputSource)     ? MergeKind.MergeWithOutputSource :
            methodCall.IsSameGenericMethod(MergeWithOutputInto)       ? MergeKind.MergeWithOutputInto :
            methodCall.IsSameGenericMethod(MergeWithOutputIntoSource) ? MergeKind.MergeWithOutputIntoSource :
                                                                        MergeKind.Merge;

        mergeContext.Kind = kind;

        if (kind != MergeKind.Merge)
        {
            var actionField   = FieldWord.FakeField(new DbDataType(typeof(string)), "$action", false);

            var (deletedContext, insertedContext, deletedTable, insertedTable) = UpdateBuilder.CreateDeletedInsertedContexts(builder, mergeContext.TargetContext, out var outputContext);

            mergeContext.Merge.Output = new OutputClause()
            {
                InsertedTable = insertedTable,
                DeletedTable  = deletedTable,
            };

            mergeContext.OutputContext = outputContext;

            var selectQuery        = outputContext.SelectQuery;
            var actionFieldContext = new SingleExpressionContext(builder, actionField, selectQuery);

            if (kind is MergeKind.MergeWithOutput or MergeKind.MergeWithOutputSource)
            {
                var outputLambda = methodCall.Arguments[1].UnwrapLambda();
                var outputExpression = SequenceHelper.PrepareBody(outputLambda, actionFieldContext, deletedContext, insertedContext);

                if (outputLambda.Parameters.Count > 3)
                {
                    outputExpression = outputExpression.Replace(outputLambda.Parameters[3],
                        mergeContext.SourceContext.SourcePropAccess);
                }

                mergeContext.OutputExpression = outputExpression;
            }
            else
            {
                var outputLambda = methodCall.Arguments[2].UnwrapLambda();
                var outputExpression = SequenceHelper.PrepareBody(outputLambda, actionFieldContext, deletedContext, insertedContext);

                if (outputLambda.Parameters.Count > 3)
                {
                    outputExpression = outputExpression.Replace(outputLambda.Parameters[3],
                        mergeContext.SourceContext.SourcePropAccess);
                }

                var outputTable = methodCall.Arguments[1];
                var destination = builder.BuildSequence(new BuildInfo(buildInfo, outputTable, new SelectQueryClause()));
                var destinationRef = new ContextRefExpression(methodCall.Method.GetGenericArguments()[2], destination);

                var outputSetters = new List<UpdateBuilder.SetExpressionEnvelope>();
                UpdateBuilder.ParseSetter(builder, destinationRef, outputExpression, outputSetters);
                UpdateBuilder.InitializeSetExpressions(builder, mergeContext.SourceContext,
                    mergeContext.TargetContext, outputSetters, mergeContext.Merge.Output.OutputItems, createColumns : false);

                mergeContext.Merge.Output.OutputTable = ((TableContext)destination).SqlTable;
                mergeContext.OutputExpression = outputExpression;
            }
        }

        return mergeContext;
    }

    static IBuildContext BuildMergeInto(ClauseSqlTranslator builder, BuildInfo buildInfo, MethodCallExpression methodCall)
    {
        var sourceContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], new SelectQueryClause()));
        var target        = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1]) { AssociationsAsSubQueries = true });

        var targetTable = MergeContext.GetTargetTable(target);
        if (targetTable == null)
            throw new NotImplementedException("Currently, only CTEs are supported as the target of a merge. You can fix by calling .AsCte() on the parameter before passing into .MergeInto().");

        var merge = new MergeSentence(targetTable);
        if (methodCall.Arguments.Count == 3)
            merge.Hint = builder.EvaluateExpression<string>(methodCall.Arguments[2]);

        target.SetAlias(merge.Target.FindAlias()!);

        var genericArguments = methodCall.Method.GetGenericArguments();

        var source = new TableLikeQueryContext(new ContextRefExpression(genericArguments[0], target, "t"),
            new ContextRefExpression(genericArguments[1], sourceContext, "s"));

        return new MergeContext(merge, target, source);
    }

    static IBuildContext BuildOn(ClauseSqlTranslator builder, BuildInfo buildInfo, MethodCallExpression methodCall)
    {
        var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

        var statement = mergeContext.Merge;

        if (methodCall.Arguments.Count == 2)
        {
            var predicate = methodCall.Arguments[1];
            var condition = predicate.UnwrapLambda();

            mergeContext.SourceContext.ConnectionLambda       = condition;

            mergeContext.SourceContext.TargetContextRef.Alias = condition.Parameters[0].Name;
            mergeContext.SourceContext.SourceContextRef.Alias = condition.Parameters[1].Name;

            var preparedCondition = mergeContext.SourceContext.GenerateCondition();

            BuildMatchCondition(builder, preparedCondition, mergeContext.SourceContext, statement.On);
        }
        else if (methodCall.Arguments.Count == 3)
        {
            var targetKeyLambda = methodCall.Arguments[1].UnwrapLambda();
            var sourceKeyLambda = methodCall.Arguments[2].UnwrapLambda();

            var targetKeySelector = mergeContext.SourceContext.PrepareTargetLambda(targetKeyLambda);
            var sourceKeySelector = mergeContext.SourceContext.PrepareSourceBody(sourceKeyLambda);

            mergeContext.SourceContext.TargetKeySelector = targetKeySelector;
            mergeContext.SourceContext.SourceKeySelector = sourceKeySelector;

            BuildMatchCondition(builder, targetKeySelector, sourceKeySelector, mergeContext.SourceContext, statement.On);
        }
        else
        {
            var targetType       = statement.Target.FindSystemType()!;
            var targetLambdaType = mergeContext.SourceContext.TargetContextRef.Type;
            var pTarget          = Expression.Parameter(targetLambdaType, "t");
            var pSource          = Expression.Parameter(targetLambdaType, "s");

            var en = mergeContext.DB.client.EntityCash.getEntityInfo(targetType);

            Expression? ex = null;

            for (var i = 0; i< en.Columns.Count; i++)
            {
                var column = en.Columns[i];
                if (!column.IsPrimarykey)
                    continue;

                var member = targetLambdaType.GetMemberEx(column.PropertyInfo);
                if (member == null)
                    throw new InvalidOperationException($"Member '{column.PropertyInfo.Name}' is not defined in '{pTarget.Name}'");

                var expr = Expression.Equal(
                    Expression.MakeMemberAccess(pTarget, member),
                    Expression.MakeMemberAccess(pSource, member));
                ex = ex != null ? Expression.AndAlso(ex, expr) : expr;
            }

            if (ex == null)
                throw new SooQueryException("Method OnTargetKey() needs at least one primary key column");

            var condition = Expression.Lambda(ex, pTarget, pSource);

            mergeContext.SourceContext.ConnectionLambda = condition;

            var generatedCondition = mergeContext.SourceContext.GenerateCondition();

            BuildMatchCondition(builder, generatedCondition, mergeContext.SourceContext, statement.On);
        }

        return mergeContext;
    }

    static IBuildContext BuildUsing(ClauseSqlTranslator builder, BuildInfo buildInfo, MethodCallExpression methodCall)
    {
        var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

        var sourceContext =
            builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQueryClause()));

        var genericArgs = methodCall.Method.GetGenericArguments();

        var source = new TableLikeQueryContext(
            new ContextRefExpression(genericArgs[0], mergeContext.TargetContext, "target"),
            new ContextRefExpression(genericArgs[1], sourceContext, "source"));

        mergeContext.Sequences    = new[] { mergeContext.Sequence, source };
        mergeContext.Merge.Source = source.Source;

        return mergeContext;
    }

    static IBuildContext BuildUsingTarget(ClauseSqlTranslator builder, BuildInfo buildInfo, MethodCallExpression methodCall)
    {
        var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

        var genericArguments = methodCall.Method.GetGenericArguments();

        var cloningContext      = new CloningContext();
        var clonedTargetContext = cloningContext.CloneContext(mergeContext.TargetContext);

        var targetContextRef = new ContextRefExpression(genericArguments[0], mergeContext.TargetContext, "target");
        var sourceContextRef = new ContextRefExpression(genericArguments[0], clonedTargetContext, "source");

        var source                = new TableLikeQueryContext(targetContextRef, sourceContextRef);
        mergeContext.Sequences    = new IBuildContext[] { mergeContext.Sequence, source };
        mergeContext.Merge.Source = source.Source;

        return mergeContext;
    }

    static IBuildContext BuildInsertWhenNotMatched(ClauseSqlTranslator builder, BuildInfo buildInfo, MethodCallExpression methodCall)
    {
        var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

        var statement = mergeContext.Merge;
        var operation = new MergeOperationClause(MergeOperateType.Insert);
        statement.Operations.Add(operation);

        var predicate = methodCall.Arguments[1];
        var setter    = methodCall.Arguments[2];

        Expression setterExpression;

        if (!setter.IsNullValue())
        {
            var setterLambda = setter.UnwrapLambda();

            setterExpression = mergeContext.SourceContext.PrepareSourceBody(setterLambda);
        }
        else
        {
            setterExpression = builder.BuildFullEntityExpression(
                builder.DBLive, mergeContext.SourceContext.SourcePropAccess,
                mergeContext.SourceContext.SourceContextRef.Type, ProjectFlags.SQL,
                EntityConstructorBase.FullEntityPurpose.Insert);
        }

        var setterExpressions = new List<UpdateBuilder.SetExpressionEnvelope>();
        UpdateBuilder.ParseSetter(builder,
            mergeContext.SourceContext.TargetContextRef.WithType(setterExpression.Type), setterExpression,
            setterExpressions);
        UpdateBuilder.InitializeSetExpressions(builder, mergeContext.TargetContext, mergeContext.SourceContext, setterExpressions, operation.Items, createColumns : false);

        if (!predicate.IsNullValue())
        {
            var condition = predicate.UnwrapLambda();

            var conditionExpr = mergeContext.SourceContext.PrepareSourceBody(condition);

            operation.Where = new SearchConditionWord();

            builder.BuildSearchCondition(
                mergeContext.SourceContext,
                conditionExpr, ProjectFlags.SQL,
                operation.Where);
        }

        return mergeContext;
    }

    static IBuildContext BuildDeleteWhenMatched(ClauseSqlTranslator builder, BuildInfo buildInfo, MethodCallExpression methodCall)
    {
        var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

        var statement = mergeContext.Merge;
        var operation = new MergeOperationClause(MergeOperateType.Delete);
        statement.Operations.Add(operation);

        var predicate = methodCall.Arguments[1];
        if (!predicate.IsNullValue())
        {
            var condition           = predicate.UnwrapLambda();
            var conditionExpression = mergeContext.SourceContext.PrepareTargetSource(condition);

            operation.Where = new SearchConditionWord();

            builder.BuildSearchCondition(
                mergeContext.SourceContext,
                conditionExpression,
                buildInfo.GetFlags(ProjectFlags.ForceOuterAssociation),
                operation.Where);
        }

        return mergeContext;
    }

    static IBuildContext BuildDeleteWhenNotMatchedBySource(ClauseSqlTranslator builder, BuildInfo buildInfo, MethodCallExpression methodCall)
    {
        var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

        var statement = mergeContext.Merge;
        var operation = new MergeOperationClause(MergeOperateType.DeleteBySource);
        statement.Operations.Add(operation);

        var predicate = methodCall.Arguments[1];
        if (!predicate.IsNullValue())
        {
            var condition          = predicate.UnwrapLambda();
            var conditionCorrected = mergeContext.SourceContext.PrepareSelfTargetLambda(condition);

            operation.Where = new SearchConditionWord();

            builder.BuildSearchCondition(mergeContext.TargetContext, conditionCorrected, ProjectFlags.SQL, operation.Where);
        }

        return mergeContext;
    }

    static IBuildContext BuildUpdateWhenMatched(ClauseSqlTranslator builder, BuildInfo buildInfo, MethodCallExpression methodCall)
    {
        var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

        var statement = mergeContext.Merge;
        var operation = new MergeOperationClause(MergeOperateType.Update);

        var predicate = methodCall.Arguments[1];
        var setter    = methodCall.Arguments[2];

        if (!setter.IsNullValue())
        {
            var setterLambda = (LambdaExpression)setter.Unwrap();

            var setterExpression = mergeContext.SourceContext.PrepareTargetSource(setterLambda);

            var setterExpressions = new List<UpdateBuilder.SetExpressionEnvelope>();
            UpdateBuilder.ParseSetter(builder,
                mergeContext.SourceContext.TargetContextRef.WithType(setterExpression.Type),
                setterExpression, setterExpressions);

            UpdateBuilder.InitializeSetExpressions(builder, mergeContext.TargetContext, mergeContext.SourceContext, setterExpressions, operation.Items, createColumns : false);
        }
        else
        {
            var sqlTable   = (TableWord)statement.Target.FindISrc();

            var sourceRef = mergeContext.SourceContext.SourcePropAccess;
            var targetRef = new ContextRefExpression(sqlTable.ObjectType, mergeContext.TargetContext);
            var keys       = sqlTable.GetKeys(false)!.Cast<FieldWord>().ToList();

            foreach (var field in sqlTable.Fields.Where(f => f.IsUpdatable).Except(keys))
            {
                var sourceMemberInfo = sourceRef.Type.GetMemberEx(field.ColumnDescriptor.PropertyInfo);
                if (sourceMemberInfo is null)
                    throw new InvalidOperationException($"Member '{field.ColumnDescriptor.PropertyInfo}' not found in type '{sourceRef.Type}'.");

                var sourceExpression = ExpressionExtensions.GetMemberGetter(sourceMemberInfo, sourceRef);
                var targetExpression = ExpressionExtensions.GetMemberGetter(field.ColumnDescriptor.PropertyInfo, targetRef);
                var tgtExpr          = builder.ConvertToSql(mergeContext.TargetContext, targetExpression);
                var srcExpr          = builder.ConvertToSql(mergeContext.SourceContext, sourceExpression);

                operation.Items.Add(new SetWord(tgtExpr, srcExpr));
            }

            if (operation.Items.Count == 0)
                return mergeContext;
        }

        statement.Operations.Add(operation);

        if (!predicate.IsNullValue())
        {
            var condition = predicate.UnwrapLambda();

            var conditionPrepared = mergeContext.SourceContext.PrepareTargetSource(condition);

            operation.Where = new SearchConditionWord();

            builder.BuildSearchCondition(mergeContext.SourceContext.SourceContextRef.BuildContext,
                conditionPrepared, ProjectFlags.SQL, operation.Where);
        }

        return mergeContext;
    }

    static IBuildContext BuildUpdateWhenMatchedThenDelete(ClauseSqlTranslator builder, BuildInfo buildInfo, MethodCallExpression methodCall)
    {
        var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

        var statement = mergeContext.Merge;
        var operation = new MergeOperationClause(MergeOperateType.UpdateWithDelete);
        statement.Operations.Add(operation);

        var predicate       = methodCall.Arguments[1];
        var setter          = methodCall.Arguments[2];
        var deletePredicate = methodCall.Arguments[3];

        if (!setter.IsNullValue())
        {
            var setterLambda = setter.UnwrapLambda();
            var setterExpression = mergeContext.SourceContext.PrepareTargetSource(setterLambda);

            mergeContext.SourceContext.TargetContextRef.Alias = setterLambda.Parameters[0].Name;
            mergeContext.SourceContext.SourceContextRef.Alias = setterLambda.Parameters[1].Name;

            var setterExpressions = new List<UpdateBuilder.SetExpressionEnvelope>();
            UpdateBuilder.ParseSetter(builder,
                mergeContext.SourceContext.TargetContextRef.WithType(setterExpression.Type), setterExpression,
                setterExpressions);
            UpdateBuilder.InitializeSetExpressions(builder, mergeContext.TargetContext, mergeContext.SourceContext, setterExpressions, operation.Items, createColumns : false);
        }
        else
        {
            var sqlTable   = (TableWord)statement.Target.FindISrc();
            var sourceProp = EnsureMergeType(mergeContext.SourceContext.SourcePropAccess, sqlTable.ObjectType);
            var targetProp = EnsureMergeType(mergeContext.SourceContext.TargetPropAccess, sqlTable.ObjectType);
            var keys       = (sqlTable.GetKeys(false) ?? Enumerable.Empty<IExpWord>()).Cast<FieldWord>().ToList();

            foreach (var field in sqlTable.Fields.Where(f => f.IsUpdatable).Except(keys))
            {
                var sourceExpr = ExpressionExtensions.GetMemberGetter(field.ColumnDescriptor.PropertyInfo, sourceProp);
                var targetExpr = ExpressionExtensions.GetMemberGetter(field.ColumnDescriptor.PropertyInfo, targetProp);

                var tgtExpr    = builder.ConvertToSql(mergeContext.SourceContext.SourceContextRef.BuildContext, targetExpr);
                var srcExpr    = builder.ConvertToSql(mergeContext.SourceContext.SourceContextRef.BuildContext, sourceExpr);

                operation.Items.Add(new SetWord(tgtExpr, srcExpr));
            }
        }

        if (!predicate.IsNullValue())
        {
            var predicateCondition = predicate.UnwrapLambda();
            var predicateConditionCorrected = mergeContext.SourceContext.PrepareTargetSource(predicateCondition);

            operation.Where = new SearchConditionWord();

            builder.BuildSearchCondition(mergeContext.SourceContext, predicateConditionCorrected,
                ProjectFlags.SQL, operation.Where);
        }

        if (!deletePredicate.IsNullValue())
        {
            var deleteCondition = deletePredicate.UnwrapLambda();
            var deleteConditionCorrected = mergeContext.SourceContext.PrepareTargetSource(deleteCondition);

            operation.WhereDelete = new SearchConditionWord();

            builder.BuildSearchCondition(mergeContext.SourceContext, deleteConditionCorrected,
                ProjectFlags.SQL, operation.WhereDelete);
        }

        return mergeContext;
    }

    static IBuildContext BuildUpdateWhenNotMatchedBySource(ClauseSqlTranslator builder, BuildInfo buildInfo, MethodCallExpression methodCall)
    {
        var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

        var statement = mergeContext.Merge;
        var operation = new MergeOperationClause(MergeOperateType.UpdateBySource);
        statement.Operations.Add(operation);

        var predicate = methodCall.Arguments[1];
        var setterLambda = methodCall.Arguments[2].UnwrapLambda();

        var setterExpression = mergeContext.SourceContext.PrepareSelfTargetLambda(setterLambda);

        var setterExpressions = new List<UpdateBuilder.SetExpressionEnvelope>();
        UpdateBuilder.ParseSetter(builder,
            mergeContext.SourceContext.TargetContextRef.WithType(setterExpression.Type), setterExpression,
            setterExpressions);

        UpdateBuilder.InitializeSetExpressions(builder, mergeContext.TargetContext, mergeContext.SourceContext, setterExpressions, operation.Items, false);

        if (!predicate.IsNullValue())
        {
            var condition          = predicate.UnwrapLambda();
            var conditionCorrected = mergeContext.SourceContext.PrepareSelfTargetLambda(condition);

            operation.Where = new SearchConditionWord();

            builder.BuildSearchCondition(mergeContext.TargetContext, conditionCorrected, ProjectFlags.SQL, operation.Where);
        }

        return mergeContext;
    }

    static void BuildMatchCondition(ClauseSqlTranslator builder, Expression condition, TableLikeQueryContext source,
        SearchConditionWord searchCondition)
    {
        BuildMatchCondition(builder, condition, null, null, source, searchCondition);
    }

    static void BuildMatchCondition(ClauseSqlTranslator builder, Expression targetKeySelector, Expression sourceKeySelector, TableLikeQueryContext source,
        SearchConditionWord searchCondition)
    {
        BuildMatchCondition(builder, null, targetKeySelector, sourceKeySelector, source, searchCondition);
    }

    static void BuildMatchCondition(ClauseSqlTranslator builder, Expression? condition, Expression? targetKeySelector, Expression? sourceKeySelector, TableLikeQueryContext source, SearchConditionWord searchCondition)
    {
        if (condition == null)
        {
            if (targetKeySelector == null || sourceKeySelector == null)
                throw new InvalidOperationException();

            if (!source.IsTargetAssociation(targetKeySelector))
            {
                var compareSearchCondition = builder.GenerateComparison(source.SourceContextRef.BuildContext, targetKeySelector, sourceKeySelector);
                searchCondition.Predicates.AddRange(compareSearchCondition.Predicates);
            }
            else
            {
                var cloningContext      = new CloningContext();
                var targetContext       = source.TargetContextRef.BuildContext;
                var clonedTargetContext = cloningContext.CloneContext(targetContext);
                var clonedContextRef    = source.TargetContextRef.WithContext(clonedTargetContext);

                var correctedTargetKeySelector = targetKeySelector.Replace(source.TargetPropAccess, clonedContextRef);

                var compareSearchCondition = builder.GenerateComparison(clonedTargetContext, correctedTargetKeySelector, sourceKeySelector);

                var selectQuery = clonedTargetContext.SelectQuery;

                selectQuery.Where.SearchCondition.Predicates.AddRange(compareSearchCondition.Predicates);

                var targetTable = MergeContext.GetTargetTable(targetContext);
                if (targetTable == null)
                    throw new NotImplementedException("Currently, only CTEs are supported as the target of a merge. You can fix by calling .AsCte() before calling .Merge()");

                var clonedTargetTable = MergeContext.GetTargetTable(clonedTargetContext);

                if (clonedTargetTable == null)
                    throw new InvalidOperationException();

                var cleanQuery = MergeContext.ReplaceSourceInQuery(selectQuery, clonedTargetTable, targetTable);

                searchCondition.AddExists(cleanQuery);
            }
        }
        else if (!source.IsTargetAssociation(condition))
        {
            builder.BuildSearchCondition(source.SourceContextRef.BuildContext, condition, ProjectFlags.SQL, searchCondition);
        }
        else
        {
            var cloningContext      = new CloningContext();
            var targetContext       = source.TargetContextRef.BuildContext;
            var clonedTargetContext = cloningContext.CloneContext(targetContext);
            var clonedContextRef    = source.TargetContextRef.WithContext(clonedTargetContext);

            var correctedCondition = condition.Replace(source.TargetPropAccess, clonedContextRef);

            builder.BuildSearchCondition(clonedTargetContext, correctedCondition, ProjectFlags.SQL,
                clonedTargetContext.SelectQuery.Where.EnsureConjunction());

            var targetTable = MergeContext.GetTargetTable(targetContext);
            if (targetTable == null)
                throw new NotImplementedException("Currently, only CTEs are supported as the target of a merge. You can fix by calling .AsCte() before calling .Merge()");

            var clonedTargetTable = MergeContext.GetTargetTable(clonedTargetContext);

            if (clonedTargetTable == null)
                throw new InvalidOperationException();

            var cleanQuery = MergeContext.ReplaceSourceInQuery(clonedTargetContext.SelectQuery, clonedTargetTable, targetTable);

            searchCondition.AddExists(cleanQuery);
        }
    }

    static Expression EnsureMergeType(Expression expression, Type type)
    {
        if (expression.Type == type)
            return expression;

        return Expression.Convert(expression, type);
    }
}
