using mooSQL.data.call;
using mooSQL.data.model;
using mooSQL.data.model.affirms;
using mooSQL.linq.Linq;
using mooSQL.linq.Expressions;
using mooSQL.linq.Extensions;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.SqlQuery;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitOfType(OfTypeCall method) => VisitOfTypeCore(method);

    MethodCall VisitOfTypeCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildOfType(methodCall))
            return method;

        var buildResult = Context.Builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
        if (buildResult.BuildContext == null)
            return method;

        var sequence = buildResult.BuildContext;
        IBuildContext? result = sequence;

        if (sequence is TableContext table
            && table.InheritanceMapping.Count > 0)
        {
            var objectType = methodCall.Type.GetGenericArguments()[0];

            if (table.ObjectType.IsSameOrParentOf(objectType))
            {
                if (!buildInfo.IsTest)
                {
                    var predicate = Context.Builder.MakeIsPredicate(table, objectType);

                    if (predicate.GetType() != typeof(Expr))
                        sequence.SelectQuery.Where.EnsureConjunction().Add(predicate);
                }

                result = new OfTypeContext(sequence, objectType);
            }
        }
        else
        {
            var toType   = methodCall.Type.GetGenericArguments()[0];
            var gargs    = methodCall.Arguments[0].Type.GetGenericArguments(typeof(IEnumerable<>));
            var fromType = gargs == null ? typeof(object) : gargs[0];

            if (toType.IsSubclassOf(fromType))
            {
                for (var type = toType.BaseType; type != null && type != typeof(object); type = type.BaseType)
                {
                    var en = Context.Builder.DBLive.client.EntityCash.getEntityInfo(type);

                    if (en != null)
                    {
                        if (!buildInfo.IsTest)
                        {
                            var predicate = MakeOfTypeIsPredicate(Context.Builder, sequence, fromType, toType);
                            sequence.SelectQuery.Where.EnsureConjunction().Add(predicate);
                        }

                        result = new OfTypeContext(sequence, toType);
                        break;
                    }
                }
            }
        }

        return ToStatementCallOr(method, result);
    }

    static bool CanBuildOfType(MethodCallExpression call)
        => call.IsQueryable();

    static IAffirmWord MakeOfTypeIsPredicate(ClauseSqlTranslator builder, IBuildContext context, Type fromType, Type toType)
    {
        var en = builder.DBLive.client.EntityCash.getEntityInfo(fromType);
        var table = new TableWord(en);

        return builder.MakeIsPredicate((context, table), context, null, toType,
            static (ctx, name) =>
            {
                var field  = ctx.table.FindFieldByMemberName(name) ?? throw new LinqException($"Field {name} not found in table {ctx.table}");
                var member = field.ColumnDescriptor.PropertyInfo;

                var contextRef = new ContextRefExpression(member.DeclaringType!, ctx.context);
                var expr       = Expression.MakeMemberAccess(contextRef, member);
                return ctx.context.Builder.ConvertToSql(contextRef.BuildContext, expr);
            });
    }
}
