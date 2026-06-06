using mooSQL.data;
using mooSQL.data.call;
using mooSQL.linq.Linq;
using mooSQL.linq.Expressions;
using mooSQL.linq.Extensions;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.Reflection;
using mooSQL.utils;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitIncludes(IncludesCall method) => VisitIncludeCore(method);
    public override MethodCall VisitThenInclude(ThenIncludeCall method) => VisitIncludeCore(method);
    public override MethodCall VisitIncludesAsTable(IncludesAsTableCall method) => VisitIncludeCore(method);
    public override MethodCall VisitIncludeInternal(IncludeInternalCall method) => VisitIncludeCore(method);

    MethodCall VisitIncludeCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildInclude(methodCall))
            return method;

        return ToStatementCallOr(method, BuildInclude(Context.Builder, buildInfo, methodCall));
    }

    static bool CanBuildInclude(MethodCallExpression call)
        => call.IsQueryable();

    static IClauseContext? BuildInclude(ClauseSqlTranslator builder, BuildInfo buildInfo, MethodCallExpression methodCall)
    {
        var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
        if (buildResult.BuildContext == null)
            return null;
        var sequence = buildResult.BuildContext;

        ITableContext? table = null;

        IncludeInfo lastLoadWith;

        if (methodCall.Method.Name == "IncludeInternal")
        {
            table = SequenceHelper.GetTableOrCteContext(sequence);

            if (table == null)
                return null;

            var loadWith     = methodCall.Arguments[1].EvaluateExpression<IncludeInfo>();
            var loadWithPath = methodCall.Arguments[2].EvaluateExpression<MemberInfo[]>();

            table.IncludeRoot = loadWith!;
            table.IncludePath = loadWithPath;
            lastLoadWith       = loadWith!;
        }
        else
        {
            var selector = methodCall.Arguments[1].UnwrapLambda();

            if (methodCall.IsQueryable("Includes"))
            {
                while (sequence is IncludeContext lw)
                    sequence = lw.Context;
            }
            else
            {
                if (sequence is IncludeContext lw)
                    table = lw.RegisterContext as ITableContext;
            }

            var path = SequenceHelper.PrepareBody(selector, sequence);

            var extractResult = ExtractAssociations(builder, table, path, null);

            if (extractResult == null)
                throw new SooQueryException($"Unable to retrieve properties path for Includes/ThenInclude. Path: '{selector}'");

            var associations = extractResult.Value.info.Length <= 1
                ? extractResult.Value.info
                : extractResult.Value.info
                    .Reverse()
                    .ToArray();

            if (associations.Length == 0)
                throw new SooQueryException($"Unable to retrieve properties path for Includes/ThenInclude. Path: '{path}'");

            table = extractResult.Value.context ?? throw new SooQueryException("Unable to find table for Includes association.");

            var tableLoadWith = table.IncludeRoot;

            if (methodCall.Method.Name == "ThenInclude")
            {
                var prevSequence = (IncludeContext)sequence;
                if (prevSequence.LastIncludeInfo == null)
                    throw new InvalidOperationException();

                var lastPath = CorrectLastPath(prevSequence.LastIncludeInfo, table.IncludePath);
                if (lastPath == null)
                    throw new SooQueryException($"ThenInclude should follow Includes. Cannot find previous property for '{path}'.");

                lastLoadWith = MergeInclude(lastPath, associations);

                if (methodCall.Arguments.Count == 3)
                {
                    var lastElement = associations[associations.Length - 1];
                    lastElement.FilterFunc = (Expression?)methodCall.Arguments[2];
                    if (lastElement.MemberInfo != null)
                        CheckFilterFunc(lastElement.MemberInfo.GetMemberType(), lastElement.FilterFunc!.Type, sequence.Builder.DBLive);
                }
            }
            else if (methodCall.Method.Name == "Includes" || methodCall.Method.Name == "IncludesAsTable")
            {
                var lastPath = CorrectLastPath(tableLoadWith, table.IncludePath);

                if (tableLoadWith == null)
                    throw new InvalidOperationException();

                if (methodCall.Arguments.Count == 3)
                {
                    var lastElement = associations[associations.Length - 1];
                    lastElement.FilterFunc = (Expression?)methodCall.Arguments[2];
                    if (lastElement.MemberInfo != null)
                        CheckFilterFunc(lastElement.MemberInfo.GetMemberType(), lastElement.FilterFunc!.Type, sequence.Builder.DBLive);
                }

                lastLoadWith = MergeInclude(lastPath, associations);
            }
            else
                throw new InvalidOperationException();
        }

        var loadWithSequence = sequence as IncludeContext ?? new IncludeContext(sequence, table!);
        loadWithSequence.LastIncludeInfo = lastLoadWith;

        RegisterIncludeNavColumns(builder, table, lastLoadWith);

        return loadWithSequence;
    }

    static void CheckFilterFunc(Type expectedType, Type filterType, DBInstance mappingSchema)
    {
        var propType = expectedType;
        if (EagerLoading.IsEnumerableType(expectedType, mappingSchema))
            propType = EagerLoading.GetEnumerableElementType(expectedType, mappingSchema);
        var itemType = typeof(Expression<>).IsSameOrParentOf(filterType) ?
            filterType.GetGenericArguments()[0].GetGenericArguments()[0].GetGenericArguments()[0] :
            filterType.GetGenericArguments()[0].GetGenericArguments()[0];
        if (propType != itemType)
            throw new LinqException("Invalid filter function usage.");
    }

    static IncludeInfo CorrectLastPath(IncludeInfo lastPath, MemberInfo[]? loadWithPath)
    {
        if (loadWithPath?.Length > 0)
        {
            var current = lastPath;
            foreach (var memberInfo in loadWithPath)
            {
                var found = current.NextInfos?.FirstOrDefault(li =>
                    MemberInfoEqualityComparer.Default.Equals(li.MemberInfo, memberInfo));
                if (found == null)
                    throw new InvalidOperationException();
                current = found;
            }

            lastPath = current;
        }

        return lastPath;
    }

    static void RegisterIncludeNavColumns(ClauseSqlTranslator builder, ITableContext? table, IncludeInfo lastLoadWith)
    {
        if (table?.ObjectType == null)
            return;

        var entityType = table.ObjectType;
        var ed = builder.DBLive.client.EntityCash.getEntityInfo(entityType);
        var info = lastLoadWith;
        while (info != null)
        {
            if (info.MemberInfo != null)
            {
                var col = ed.Columns.FirstOrDefault(c =>
                    c.PropertyInfo != null && c.PropertyInfo.Name == info.MemberInfo.Name);
                if (col != null)
                    builder.RegisterNavColumn(entityType, col);
            }
            info = info.NextInfos?.FirstOrDefault();
        }
    }

    static (ITableContext? context, IncludeInfo[] info)? ExtractAssociations(ClauseSqlTranslator builder, ITableContext? parentContext, Expression expression, Expression? stopExpression)
    {
        var currentExpression = expression;

        while (currentExpression.NodeType == ExpressionType.Call)
        {
            var mc = (MethodCallExpression)currentExpression;
            if (mc.IsQueryable())
                currentExpression = mc.Arguments[0];
            else
                break;
        }

        LambdaExpression? filterExpression = null;
        if (currentExpression != expression)
        {
            var parameter  = Expression.Parameter(currentExpression.Type, "e");

            var body   = expression.Replace(currentExpression, parameter);
            var lambda = Expression.Lambda(body, parameter);

            filterExpression = lambda;
        }

        var (context, members) = GetAssociations(builder, parentContext, currentExpression, stopExpression);
        if (context == null)
            return default;

        var loadWithInfos = members
            .Select((m, i) => new IncludeInfo(m, true) { MemberFilter = i == 0 ? filterExpression : null })
            .ToArray();

        return (context, loadWithInfos);
    }

    static (ITableContext? context, List<MemberInfo> members) GetAssociations(ClauseSqlTranslator builder, ITableContext? parentContext, Expression expression, Expression? stopExpression)
    {
        ITableContext? context    = parentContext;
        MemberInfo?    lastMember = null;

        var members = new List<MemberInfo>();
        var stop    = false;

        for (;;)
        {
            if (stopExpression == expression || stop)
            {
                break;
            }

            switch (expression.NodeType)
            {
                case ExpressionType.Parameter :
                {
                    if (lastMember == null)
                        goto default;
                    stop = true;
                    break;
                }

                case ExpressionType.Call      :
                {
                    var cexpr = (MethodCallExpression)expression;

                    if (cexpr.Method.IsSqlPropertyMethodEx())
                    {
                        var memberInfo   = MemberHelper.GetMemberInfo(cexpr);
                        var memberAccess = Expression.MakeMemberAccess(cexpr.Arguments[0], memberInfo);
                        expression = memberAccess;

                        continue;
                    }

                    if (lastMember == null)
                        goto default;

                    var expr  = cexpr.Object;

                    if (expr == null)
                    {
                        if (cexpr.Arguments.Count == 0)
                            goto default;

                        expr = cexpr.Arguments[0];
                    }

                    if (expr.NodeType != ExpressionType.MemberAccess)
                        goto default;

                    var member = ((MemberExpression)expr).Member;
                    var mtype  = member.GetMemberType();

                    if (lastMember.ReflectedType != mtype.GetItemType())
                        goto default;

                    expression = expr;

                    break;
                }

                case ExpressionType.MemberAccess :
                {
                    expression = builder.BuildProjection(context, expression, ProjectFlags.Traverse);

                    if (expression.NodeType != ExpressionType.MemberAccess)
                        break;

                    var mexpr         = (MemberExpression)expression;
                    var member        = lastMember = mexpr.Member;
                    var isAssociation = builder.IsAssociation(expression, out _);

                    if (!isAssociation)
                    {
                        var projected = builder.BuildProjection(context, expression, ProjectFlags.Traverse);
                        if (ExpressionEqualityComparer.Instance.Equals(projected, expression))
                            throw new SooQueryException($"Member '{expression}' is not an association.");
                        expression = projected;
                        break;
                    }

                    members.Add(member);

                    expression = mexpr.Expression!;

                    break;
                }

                case ExpressionType.ArrayIndex   :
                {
                    expression = ((BinaryExpression)expression).Left;
                    break;
                }

                case ExpressionType.Extension    :
                {
                    if (expression is GetItemExpression getItemExpression)
                    {
                        expression = getItemExpression.Expression;
                        break;
                    }

                    if (expression is ContextRefExpression contextRef)
                    {
                        var newExpression = builder.BuildProjection(context, expression, ProjectFlags.Table);
                        if (!ReferenceEquals(newExpression, expression))
                        {
                            expression = newExpression;
                        }
                        else
                        {
                            stop    = true;
                            context = contextRef.BuildContext as ITableContext;
                        }

                        break;
                    }

                    goto default;
                }

                case ExpressionType.Convert       :
                case ExpressionType.ConvertChecked:
                {
                    expression = ((UnaryExpression)expression).Operand;
                    break;
                }

                default :
                {
                    throw new SooQueryException($"Expression '{expression}' is not an association.");
                }
            }
        }

        return (context ?? parentContext, members);
    }

    static IncludeInfo MergeInclude(IncludeInfo loadWith, IncludeInfo[] defined)
    {
        var current = loadWith;

        for (var index = 0; index < defined.Length; index++)
        {
            current.NextInfos ??= new List<IncludeInfo>();

            var d = defined[index];
            var found = current.NextInfos.FirstOrDefault(lw =>
                MemberInfoEqualityComparer.Default.Equals(lw.MemberInfo, d.MemberInfo));

            if (found != null)
            {
                found.ShouldLoad = true;

                if (index == defined.Length - 1)
                {
                    found.FilterFunc   = d.FilterFunc;
                    found.MemberFilter = d.MemberFilter;
                }

                current = found;
            }
            else
            {
                current.NextInfos.Add(d);
                current = d;
            }
        }

        return current;
    }
}
