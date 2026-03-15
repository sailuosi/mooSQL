using mooSQL.data.model;
using mooSQL.linq;
using mooSQL.linq.Common.Internal;
using mooSQL.linq.Expressions;
using mooSQL.linq.Extensions;
using mooSQL.linq.Linq;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.Mapping;
using mooSQL.linq.Reflection;
using mooSQL.linq.SqlQuery;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.linq.visitor
{
    internal class ExpWordReadVisitor:ExpressionVisitor
    {
        Dictionary<SqlCacheKey, Expression> _expressionCache = new(SqlCacheKey.SqlCacheKeyComparer);
        Dictionary<SqlCacheKey, Expression> _cachedSql = new(SqlCacheKey.SqlCacheKeyComparer);
        ExpressionTreeOptimizationContext optimizationContext;
        public DBInstance DBLive {  get; set; }

        public ExpressionBuilder ExpBuilder { get; set; }
#if DEBUG
        int _makeCounter;
#endif
        /// <summary>
        /// 缓存创建的表达式
        /// </summary>
        /// <param name="forContext"></param>
        /// <param name="path"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        public Expression MakeExpression(IBuildContext? forContext, Expression path, ProjectFlags flags)
        {
            var currentContext = forContext;
#if DEBUG
            Expression ExecuteMake(IBuildContext context, Expression expr, ProjectFlags projectFlags)
            {
                var counter = ++_makeCounter;

                //Debug.WriteLine($"({counter.ToString(CultureInfo.InvariantCulture)})ExecuteMake ({projectFlags}):");
                //Debug.WriteLine($"\tCtx: {BuildContextDebuggingHelper.GetContextInfo(currentContext)}");
                //Debug.WriteLine($"\tPath: {path}");
                //Debug.WriteLine($"\tExpr: {expr}");

                var result = context.MakeExpression(expr, projectFlags);

                //Debug.WriteLine($"({counter.ToString(CultureInfo.InvariantCulture)})Result ({projectFlags}): {result}");
                //Debug.WriteLine("");

                return result;
            }
#else
			static Expression ExecuteMake(IBuildContext context, Expression expr, ProjectFlags projectFlags)
			{
				return context.MakeExpression(expr, projectFlags);
			}
#endif

#if DEBUG
            static void DebugCacheHit(IBuildContext? context, Expression original, Expression cached, ProjectFlags projectFlags)
            {
                Debug.WriteLine($"Cache hit for: {original}, {projectFlags}");
                Debug.WriteLine($"\tResult: {cached}");

                if (!projectFlags.IsTest() && (projectFlags.IsExpression() || projectFlags.IsSql()))
                {
                }
            }
#endif

            ContextRefExpression? CalcRootContext(Expression expressionToCheck)
            {
                expressionToCheck = expressionToCheck.UnwrapConvert();

                if (expressionToCheck is ContextRefExpression contextRef)
                    return contextRef;

                if (expressionToCheck is MemberExpression me)
                {
                    if (me.Expression is null)
                        return null;
                    return CalcRootContext(me.Expression);
                }

                if (expressionToCheck is MethodCallExpression mc && mc.IsQueryable())
                {
                    return CalcRootContext(mc.Arguments[0]);
                }

                return null;
            }

            // 这里没有什么可投射的
            if (path.NodeType == ExpressionType.Parameter
                || path.NodeType == ExpressionType.Lambda
                || path.NodeType == ExpressionType.Extension && path is SqlPlaceholderExpression or SqlGenericConstructorExpression)
            {
                return path;
            }

            if ((flags & (ProjectFlags.Root | ProjectFlags.AggregationRoot | ProjectFlags.AssociationRoot | ProjectFlags.ExtractProjection | ProjectFlags.Table)) == 0)
            {
                // 尝试找到已经转换为SQL的
                var sqlKey = new SqlCacheKey(path, null, null, forContext?.SelectQuery, flags.SqlFlag());
                if (_cachedSql.TryGetValue(sqlKey, out var cachedSql) && cachedSql is SqlPlaceholderExpression)
                {
                    return cachedSql;
                }
            }

            var shouldCache = !flags.IsTest() && null != path.Find(1, (_, e) => e is ContextRefExpression);

            var key = new SqlCacheKey(path, null, null, forContext?.SelectQuery, flags);

            Expression? expression;

            if (shouldCache && _expressionCache.TryGetValue(key, out expression) && expression.Type == path.Type && expression is not SqlErrorExpression)
            {
                if (!mooSQL.linq.ExpSameCheckor.Instance.Equals(path, expression))
                {
#if DEBUG
                    DebugCacheHit(currentContext, path, expression, flags);
#endif
                    return expression;
                }
            }

            var doNotProcess = false;
            expression = null;

            ContextRefExpression? rootContext = null;

            if (path is MemberExpression { Expression: not null } memberExpression)
            {
                var declaringType = memberExpression.Member.DeclaringType;
                if (declaringType != null && declaringType != memberExpression.Expression.Type)
                {
                    memberExpression = memberExpression.Update(SequenceHelper.EnsureType(memberExpression.Expression, declaringType));
                    return MakeExpression(currentContext, memberExpression, flags);
                }

                if (memberExpression.Member.IsNullableValueMember())
                {
                    var corrected = MakeExpression(currentContext, memberExpression.Expression, flags);
                    if (corrected.Type != path.Type)
                    {
                        corrected = Expression.Convert(corrected, path.Type);
                    }
                    return MakeExpression(currentContext, corrected, flags);
                }

                if (memberExpression.Expression.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
                {
                    var unary = (UnaryExpression)memberExpression.Expression;
                    if (unary.Operand is ContextRefExpression contextRef)
                    {
                        memberExpression = memberExpression.Update(contextRef.WithType(memberExpression.Expression.Type));
                        return MakeExpression(currentContext, memberExpression, flags);
                    }
                }

                rootContext = CalcRootContext(memberExpression.Expression);

                if (rootContext != null)
                {
                    currentContext = rootContext.BuildContext;

                    // SetOperationContext可以不需要准备就知道如何处理这样的路径

                    var corrected = ExecuteMake(rootContext.BuildContext, path, flags);

                    if (!mooSQL.linq.ExpSameCheckor.Instance.Equals(corrected, path) &&
                        corrected is not DefaultValueExpression && corrected is not SqlErrorExpression)
                    {
                        var newCorrected = MakeExpression(rootContext.BuildContext, corrected, flags);

                        if (newCorrected is SqlErrorExpression sqlError)
                        {
                            if (sqlError.IsCritical)
                                return sqlError;
                            newCorrected = corrected;
                        }

                        if (newCorrected is SqlPlaceholderExpression placeholder)
                        {
                            newCorrected = placeholder.WithTrackingPath(path);
                        }

                        if (mooSQL.linq.ExpSameCheckor.Instance.Equals(corrected, newCorrected))
                            return corrected;

                        return MakeExpression(rootContext.BuildContext, newCorrected, flags);
                    }
                }

                var root = MakeExpression(currentContext, memberExpression.Expression, flags.RootFlag());

                // 关联可能导致这种情况
                if (root is SqlErrorExpression rootError)
                {
                    return rootError.WithType(path.Type);
                }

                if (root is MethodCallExpression mce && mce.IsQueryable() && currentContext != null)
                {
                    var subqueryExpression = TryGetSubQueryExpression(currentContext, root, null, flags, out var isSequence, out var corrected);
                    if (subqueryExpression != null)
                    {
                        root = subqueryExpression;
                        if (subqueryExpression.Type != root.Type)
                        {
                            root = SqlAdjustTypeExpression.AdjustType(root, root.Type, DBLive);
                        }
                    }
                    else if (isSequence)
                    {
                        if (corrected != null)
                        {
                            // 构建序列失败，但我们转换了First/FirstOrDefault。
                            return memberExpression.Update(corrected);
                        }

                        // 构建序列失败。不要继续。
                        return memberExpression;
                    }

                }

                var newPath = memberExpression;
                if (!ReferenceEquals(root, memberExpression.Expression))
                {
                    newPath = memberExpression.Update(SequenceHelper.EnsureType(root, memberExpression.Expression.Type));
                }

                path = newPath;

                if (!flags.IsTraverse() && IsAssociation(newPath, out _))
                {
                    if (root is ContextRefExpression contextRef)
                    {
                        expression = TryCreateAssociation(newPath, contextRef, currentContext, flags);
                        if (expression is SqlErrorExpression)
                            return expression;
                    }
                }

                rootContext = CalcRootContext(root);
            }
            else if (path is MethodCallExpression mc)
            {
                if (mc.Method.IsSqlPropertyMethodEx())
                {
                    var memberInfo = MemberHelper.GetMemberInfo(mc);
                    var memberAccess = Expression.MakeMemberAccess(mc.Arguments[0], memberInfo);
                    return MakeExpression(currentContext, memberAccess, flags);
                }

                if (mc.Method.Name == nameof(Sql.Alias) && mc.Method.DeclaringType == typeof(Sql))
                {
                    var translated = MakeExpression(currentContext, mc.Arguments[0], flags);
                    if (ReferenceEquals(mc.Arguments[0], translated))
                    {
                        translated = mc;
                    }
                    else if (translated is SqlPlaceholderExpression placeholder)
                    {
                        translated = placeholder.WithAlias(mc.Arguments[1].EvaluateExpression() as string);
                    }
                    else
                    {
                        if (!flags.IsRoot())
                        {
                            var args = mc.Arguments.ToArray();
                            args[0] = translated;
                            translated = mc.Update(mc.Object, args);
                        }
                    }

                    return translated;
                }

                if (IsAssociation(mc, out _))
                {
                    var arguments = mc.Arguments;
                    if (arguments.Count == 0)
                        throw new InvalidOperationException("Association methods should have at least one parameter");

                    var firstArgument = mc.Method.IsStatic ? arguments[0] : mc.Object!;

                    if (firstArgument == null)
                        throw new InvalidOperationException();

                    var rootArgument = MakeExpression(currentContext, firstArgument, flags.RootFlag());

                    if (!ReferenceEquals(rootArgument, firstArgument))
                    {
                        if (mc.Method.IsStatic)
                        {
                            var argumentsArray = arguments.ToArray();
                            argumentsArray[0] = rootArgument;

                            mc = mc.Update(mc.Object, argumentsArray);
                        }
                        else
                        {
                            mc = mc.Update(rootArgument, mc.Arguments);
                        }
                    }

                    if (rootArgument is ContextRefExpression contextRef)
                    {
                        expression = TryCreateAssociation(mc, contextRef, currentContext, flags);
                        rootContext = expression as ContextRefExpression;
                    }
                }
            }
            else if (path is ContextRefExpression contextRef)
            {
                rootContext = contextRef;
            }
            else if (path is SqlGenericParamAccessExpression paramAccessExpression)
            {
                var root = paramAccessExpression.Constructor;
                while (root is SqlGenericParamAccessExpression pa)
                {
                    root = pa.Constructor;
                }

                if (root is ContextRefExpression contextRefExpression)
                {
                    rootContext = contextRefExpression;
                }
            }
            else if (path.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
            {
                var unary = (UnaryExpression)path;

                expression = MakeExpression(currentContext, unary.Operand, flags);
                if (!flags.IsTable() && expression.Type != path.Type)
                {
                    expression = Expression.MakeUnary(path.NodeType, expression, unary.Type, unary.Method);
                }
                doNotProcess = true;
            }
            else if (path.NodeType == ExpressionType.TypeAs && currentContext != null)
            {
                var unary = (UnaryExpression)path;
                //var testExpr = MakeIsPredicateExpression(currentContext, Expression.TypeIs(unary.Operand, unary.Type));
                var testExpr = this.Visit(Expression.TypeIs(unary.Operand, unary.Type));
                var trueCase = Expression.Convert(unary.Operand, unary.Type);
                var falseCase = new DefaultValueExpression(DBLive, unary.Type);

                if (testExpr is ConstantExpression constExpr)
                {
                    if (constExpr.Value is true)
                        expression = trueCase;
                    else
                        expression = falseCase;
                }
                else
                {
                    doNotProcess = true;
                    expression = Expression.Condition(testExpr, trueCase, falseCase);
                }
            }
            else if (path.NodeType == ExpressionType.TypeIs && currentContext != null)
            {
                var typeBinary = (TypeBinaryExpression)path;
                //expression = MakeIsPredicateExpression(currentContext, typeBinary);
                expression = this.Visit(typeBinary);
                doNotProcess = true;
            }

            if (expression == null)
            {
                if (rootContext != null)
                {
                    currentContext = rootContext.BuildContext;
                    expression = ExecuteMake(currentContext, path, flags);
                }
                else
                    expression = path;
            }

            if (!doNotProcess)
            {
                if (!mooSQL.linq.ExpSameCheckor.Instance.Equals(expression, path))
                {
                    // Do recursive again
                    var convertedAgain = MakeExpression(currentContext, expression, flags);
                    if (convertedAgain is not SqlErrorExpression)
                        expression = convertedAgain;
                }
                else
                {
                    var handled = false;

                    if (flags.IsExpression() && path.NodeType == ExpressionType.NewArrayInit)
                    {
                        expression = path;
                        handled = true;
                    }

                    if (!handled && (flags.IsSql() || flags.IsExpression()))
                    {
                        // Handling subqueries
                        //

                        var ctx = rootContext?.BuildContext ?? currentContext;
                        if (ctx != null)
                        {
                            var subqueryExpression = TryGetSubQueryExpression(ctx, path, null, flags, out var isSequence, out var corrected);
                            if (subqueryExpression != null)
                            {
                                if (subqueryExpression is SqlErrorExpression)
                                {
                                    expression = subqueryExpression;
                                }
                                else
                                {
                                    expression = MakeExpression(ctx, subqueryExpression, flags);
                                    if (expression.Type != path.Type)
                                    {
                                        expression = SqlAdjustTypeExpression.AdjustType(expression, path.Type, DBLive);
                                    }
                                }

                                handled = true;
                            }
                            else if (isSequence)
                            {
                                if (corrected != null)
                                {
                                    // Failed to build sequence, but we transformed First/FirstOrDefault.
                                    expression = corrected;
                                }

                                handled = true;
                            }
                        }
                    }

                    if (!handled && flags.HasFlag(ProjectFlags.Expression) && optimizationContext.CanBeCompiled(path, true))
                    {
                        expression = path;
                        handled = true;
                    }

                }
            }

            if (expression is SqlPlaceholderExpression placeholderExpression)
            {
                expression = placeholderExpression.WithTrackingPath(path);
            }

            if (shouldCache)
            {
                _expressionCache[key] = expression;

                if (!flags.HasFlag(ProjectFlags.Test))
                {
                    if ((flags.HasFlag(ProjectFlags.SQL) ||
                         flags.HasFlag(ProjectFlags.Keys)) && expression is SqlPlaceholderExpression)
                    {
                        var anotherKey = new SqlCacheKey(path, null, null, forContext?.SelectQuery, ProjectFlags.Expression);
                        _expressionCache[anotherKey] = expression;

                        if (flags.HasFlag(ProjectFlags.Keys))
                        {
                            anotherKey = new SqlCacheKey(path, null, null, forContext?.SelectQuery, ProjectFlags.Expression | ProjectFlags.Keys);
                            _expressionCache[anotherKey] = expression;
                        }
                    }
                }
            }

            return expression;
        }
        public Expression? TryGetSubQueryExpression(IBuildContext context, Expression expr, string? alias, ProjectFlags flags, out bool isSequence, out Expression? corrected)
        {
            isSequence = false;
            corrected = null;

            if (flags.IsTraverse())
                return null;

            var unwrapped = expr.Unwrap();

            if (unwrapped is SqlErrorExpression)
                return expr;

            if (unwrapped is BinaryExpression or ConditionalExpression or DefaultExpression or DefaultValueExpression or SqlDefaultIfEmptyExpression)
                return null;

            if (unwrapped is SqlGenericConstructorExpression or ConstantExpression or SqlEagerLoadExpression)
                return null;

            if (unwrapped is ContextRefExpression contextRef && contextRef.BuildContext.ElementType == expr.Type)
                return null;

            if (SequenceHelper.IsSpecialProperty(unwrapped, out _, out _))
                return null;

            if (!flags.IsSubquery())
            {
                if (optimizationContext.CanBeCompiled(unwrapped, true))
                    return null;

                if (unwrapped is MemberInitExpression or NewExpression or NewArrayExpression)
                {
                    var withDetails = TranslateDetails(context, unwrapped, flags);
                    if (optimizationContext.CanBeCompiled(withDetails, true))
                        return null;
                }
            }

            if (unwrapped is MemberExpression me)
            {
                var attr = me.Member.GetExpressionAttribute(DBLive);
                if (attr != null)
                    return null;
            }

            var info = GetSubQueryContext(context, ref context, unwrapped, flags);
            isSequence = info.IsSequence;

            if (info.Context == null)
            {
                if (isSequence)
                {
                    if (flags.IsExpression())
                    {
                        // Trying to relax eager for First[OrDefault](predicate)
                        var prepared = PrepareSubqueryExpression(expr);
                        if (!ReferenceEquals(prepared, expr))
                        {
                            corrected = prepared;
                        }

                        return null;
                    }

                    return new SqlErrorExpression(expr, info.ErrorMessage, expr.Type);
                }

                return null;
            }

            if (!IsSingleElementContext(info.Context) && expr.Type.IsEnumerableType(info.Context.ElementType) && !flags.IsExtractProjection())
            {
                var eager = (Expression)new SqlEagerLoadExpression(unwrapped);
                eager = SqlAdjustTypeExpression.AdjustType(eager, expr.Type, DBLive);

                return eager;
            }

            var resultExpr = (Expression)new ContextRefExpression(unwrapped.Type, info.Context);

            return resultExpr;
        }
        public static bool IsSingleElementContext(IBuildContext context)
        {
            return context is FirstSingleBuilder.FirstSingleContext;
        }
        static ObjectPool<BuildVisitor> _buildVisitorPool = new(() => new BuildVisitor(), v => v.Cleanup(), 100);
        Expression TranslateDetails(IBuildContext context, Expression expr, ProjectFlags flags)
        {
            using var visitor = _buildVisitorPool.Allocate();
            var newExpr = visitor.Value.Build(context, expr, flags, BuildFlags.ForceAssignments | BuildFlags.IgnoreRoot);
            return newExpr;
        }
        Dictionary<SqlCacheKey, Expression>? _associations;
        public Expression TryCreateAssociation(Expression expression, ContextRefExpression rootContext, IBuildContext? forContext, ProjectFlags flags)
        {
            var associationDescriptor = GetAssociationDescriptor(expression, out var memberInfo);

            if (associationDescriptor == null || memberInfo == null)
                return expression;

            var associationRoot = (ContextRefExpression)MakeExpression(rootContext.BuildContext, rootContext, flags.AssociationRootFlag());

            _associations ??= new Dictionary<SqlCacheKey, Expression>(SqlCacheKey.SqlCacheKeyComparer);

            var cacheFlags = flags.RootFlag() & ~(ProjectFlags.Subquery | ProjectFlags.ExtractProjection | ProjectFlags.ForceOuterAssociation);
            var key = new SqlCacheKey(expression, associationRoot.BuildContext, null, null, cacheFlags);

            if (_associations.TryGetValue(key, out var associationExpression))
                return associationExpression;

            LoadWithInfo? loadWith = null;
            MemberInfo[]? loadWithPath = null;

            var prevIsOuter = flags.HasFlag(ProjectFlags.ForceOuterAssociation);
            bool? isOptional = prevIsOuter ? true : null;

            if (rootContext.BuildContext.IsOptional)
                isOptional = true;

            var table = SequenceHelper.GetTableOrCteContext(rootContext.BuildContext);

            if (table != null)
            {
                loadWith = table.LoadWithRoot;
                loadWithPath = table.LoadWithPath;
                if (table.IsOptional)
                    isOptional = true;
            }

            if (forContext?.IsOptional == true)
                isOptional = true;

            if (associationDescriptor.IsList)
            {
                /*if (_isOuterAssociations?.Contains(rootContext) == true)
					isOuter = true;*/
            }
            else
            {
                isOptional = isOptional == true || associationDescriptor.CanBeNull;
            }

            Expression? notNullCheck = null;
            if (associationDescriptor.IsList && (prevIsOuter || flags.IsSubquery()) && !flags.IsExtractProjection())
            {
                var keys = MakeExpression(forContext, rootContext, flags.SqlFlag().KeyFlag());
                if (forContext != null)
                {
                    notNullCheck = ExtractNotNullCheck(forContext, keys, flags.SqlFlag());
                }
            }

            var association = AssociationHelper.BuildAssociationQuery(ExpBuilder, rootContext, memberInfo,
                associationDescriptor, notNullCheck, !associationDescriptor.IsList, loadWith, loadWithPath, ref isOptional);

            associationExpression = association;

            if (!associationDescriptor.IsList && !flags.IsSubquery() && !flags.IsExtractProjection())
            {
                // IsAssociation will force to create OuterApply instead of subquery. Handled in FirstSingleContext
                //
                var buildInfo = new BuildInfo(forContext, association, new SelectQueryClause())
                {
                    IsTest = flags.IsTest(),
                    SourceCardinality = isOptional == true ? SourceCardinality.ZeroOrOne : SourceCardinality.One,
                    IsAssociation = true
                };

                var sequence = ExpBuilder. BuildSequence(buildInfo);

                //if (!flags.IsTest())
                //{
                //    if (!IsSupportedSubquery(rootContext.BuildContext, sequence, out var errorMessage))
                //        return new SqlErrorExpression(null, expression, errorMessage, expression.Type, true);
                //}

                sequence.SetAlias(associationDescriptor.GenerateAlias());

                if (forContext != null)
                    sequence = new ScopeContext(sequence, forContext);

                associationExpression = new ContextRefExpression(association.Type, sequence);
            }
            else
            {
                associationExpression = SqlAdjustTypeExpression.AdjustType(associationExpression, expression.Type, DBLive);
            }

            if (!flags.IsExtractProjection())
                _associations[key] = associationExpression;

            return associationExpression;
        }
        Expression? ExtractNotNullCheck(IBuildContext context, Expression expr, ProjectFlags flags)
        {
            SqlPlaceholderExpression? notNull = null;

            if (expr is SqlPlaceholderExpression placeholder)
            {
                notNull = placeholder.MakeNullable();
            }

            if (notNull == null)
            {
                List<Expression> expressions = new();
                //if (!CollectNullCompareExpressions(context, expr, expressions) || expressions.Count == 0)
                //    return null;

                List<SqlPlaceholderExpression> placeholders = new(expressions.Count);

                foreach (var expression in expressions)
                {
                    //var predicateExpr = ConvertToSqlExpr(context, expression, flags.SqlFlag());
                    var predicateExpr = Visit( expression);
                    if (predicateExpr is SqlPlaceholderExpression current)
                    {
                        placeholders.Add(current);
                    }
                }

                notNull = placeholders
                    .FirstOrDefault(pl => !pl.Sql.CanBeNullable(NullabilityContext.NonQuery));
            }

            if (notNull == null)
            {
                return null;
            }

            var notNullPath = notNull.Path;

            if (notNullPath.Type.IsValueType && !notNullPath.Type.IsNullable())
            {
                notNullPath = Expression.Convert(notNullPath, typeof(Nullable<>).MakeGenericType(notNullPath.Type));
            }

            var notNullExpression = Expression.NotEqual(notNullPath, Expression.Constant(null, notNullPath.Type));

            return notNullExpression;

        }

        public bool IsAssociation(Expression expression, out MemberInfo? associationMember)
        {
            switch (expression)
            {
                case MemberExpression memberExpression:
                    return IsAssociationInRealization(memberExpression.Expression, memberExpression.Member, out associationMember);

                case MethodCallExpression methodCall:
                    return IsAssociationInRealization(methodCall.Object, methodCall.Method, out associationMember);

                default:
                    associationMember = null;
                    return false;
            }
        }

        bool IsAssociationInRealization(Expression? expression, MemberInfo member,  out MemberInfo? associationMember)
        {
            if (InternalExtensions.IsAssociation(member, DBLive))
            {
                associationMember = member;
                return true;
            }

            if (expression?.Type.IsInterface == true)
            {
                if (expression is ContextRefExpression contextRef && contextRef.BuildContext.ElementType != expression.Type)
                {
                    var newMember = contextRef.BuildContext.ElementType.GetMemberEx(member);
                    if (newMember != null)
                    {
                        if (InternalExtensions.IsAssociation(newMember, DBLive))
                        {
                            associationMember = newMember;
                            return true;
                        }
                    }
                }
            }

            associationMember = null;
            return false;
        }
        AssociationDescriptor? GetAssociationDescriptor(Expression expression, out AccessorMember? memberInfo, bool onlyCurrent = true)
        {
            memberInfo = null;

            Type objectType;
            if (expression is MemberExpression memberExpression)
            {
                if (!IsAssociationInRealization(memberExpression.Expression, memberExpression.Member,
                        out var associationMember))
                    return null;

                var type = associationMember.ReflectedType ?? associationMember.DeclaringType;
                if (type == null)
                    return null;
                objectType = type;
            }
            else if (expression is MethodCallExpression methodCall)
            {
                if (!IsAssociationInRealization(methodCall.Object, methodCall.Method, out var associationMember))
                    return null;

                var type = methodCall.Method.IsStatic ? methodCall.Arguments[0].Type : associationMember.DeclaringType;
                if (type == null)
                    return null;
                objectType = type;
            }
            else
                return null;

            if (expression.NodeType == ExpressionType.MemberAccess || expression.NodeType == ExpressionType.Call)
                memberInfo = new AccessorMember(expression);

            if (memberInfo == null)
                return null;

            var entityDescriptor = DBLive.client.EntityCash.getEntityInfo(objectType);

            //var descriptor = GetAssociationDescriptor(memberInfo,out entityDescriptor);
            //if (descriptor == null && !onlyCurrent && memberInfo.MemberInfo.DeclaringType != entityDescriptor.ObjectType)
            //	descriptor = GetAssociationDescriptor(memberInfo, MappingSchema.GetEntityDescriptor(memberInfo.MemberInfo.DeclaringType!));

            //return descriptor;
            return null;
        }
        static string[] _singleElementMethods =
        {
            nameof(Enumerable.FirstOrDefault),
            nameof(Enumerable.First),
            nameof(Enumerable.Single),
            nameof(Enumerable.SingleOrDefault),
        };
        public Expression PrepareSubqueryExpression(Expression expr)
        {
            var newExpr = expr;

            if (expr.NodeType == ExpressionType.Call)
            {
                var mc = (MethodCallExpression)expr;
                if (mc.IsQueryable(_singleElementMethods))
                {
                    //is [var a0, var a1]
                    if (mc.Arguments.Count == 2)
                    {
                        var a0 = mc.Arguments[0];
                        var a1 = mc.Arguments[1];
                        Expression whereMethod;

                        var typeArguments = mc.Method.GetGenericArguments();
                        if (mc.Method.DeclaringType == typeof(Queryable))
                        {
                            var methodInfo = Methods.Queryable.Where.MakeGenericMethod(typeArguments);
                            whereMethod = Expression.Call(methodInfo, a0, a1);
                            var limitCall = Expression.Call(typeof(Queryable), mc.Method.Name, typeArguments, whereMethod);

                            newExpr = limitCall;
                        }
                        else
                        {
                            var methodInfo = Methods.Enumerable.Where.MakeGenericMethod(typeArguments);
                            whereMethod = Expression.Call(methodInfo, a0, a1);
                            var limitCall = Expression.Call(typeof(Enumerable), mc.Method.Name, typeArguments, whereMethod);

                            newExpr = limitCall;
                        }
                    }
                }
            }

            return newExpr;
        }

        Dictionary<SubqueryCacheKey, SubQueryContextInfo>? _buildContextCache;
        Dictionary<SubqueryCacheKey, SubQueryContextInfo>? _testBuildContextCache;
        SubQueryContextInfo GetSubQueryContext(IBuildContext inContext, ref IBuildContext context, Expression expr, ProjectFlags flags)
        {
            context = inContext;
            var testExpression = ExpBuilder. CorrectRoot(context, expr);
            var cacheKey = new SubqueryCacheKey(context.SelectQuery, testExpression);

            var shouldCache = flags.IsSql() || flags.IsExpression() || flags.IsExtractProjection() || flags.IsRoot();

            if (shouldCache && _buildContextCache?.TryGetValue(cacheKey, out var item) == true)
                return item;

            if (flags.IsTest())
            {
                if (_testBuildContextCache?.TryGetValue(cacheKey, out var testItem) == true)
                    return testItem;
            }

            var rootQuery = ExpBuilder.GetRootContext(context, testExpression, false);
            rootQuery ??= ExpBuilder.GetRootContext(context, expr, false);

            if (rootQuery != null)
            {
                context = rootQuery.BuildContext;
            }

            var correctedForBuild = testExpression;
            var ctx = GetSubQuery(context, correctedForBuild, flags, out var isSequence, out var errorMessage);

            var info = new SubQueryContextInfo { SequenceExpression = testExpression, Context = ctx, IsSequence = isSequence, ErrorMessage = errorMessage };

            if (shouldCache)
            {
                if (flags.IsTest())
                {
                    _testBuildContextCache ??= new(SubqueryCacheKey.Comparer);
                    _testBuildContextCache[cacheKey] = info;
                }
                else
                {
                    _buildContextCache ??= new(SubqueryCacheKey.Comparer);
                    _buildContextCache[cacheKey] = info;
                }
            }

            return info;
        }

        int _gettingSubquery;
        public IBuildContext? GetSubQuery(IBuildContext context, Expression expr, ProjectFlags flags, out bool isSequence, out string? errorMessage)
        {
            var info = new BuildInfo(context, expr, new SelectQueryClause())
            {
                CreateSubQuery = true,
            };

            if (flags.IsForceOuter())
            {
                info.SourceCardinality = SourceCardinality.ZeroOrMany;
            }

            ++_gettingSubquery;
            var buildResult = ExpBuilder.TryBuildSequence(info);
            --_gettingSubquery;

            isSequence = buildResult.IsSequence;

            if (buildResult.BuildContext != null)
            {
                if (_gettingSubquery == 0)
                {
                    ++_gettingSubquery;
                    var isSupported = ExpBuilder.IsSupportedSubquery(context, buildResult.BuildContext, out errorMessage);
                    --_gettingSubquery;
                    if (!isSupported)
                        return null;
                }
            }

            errorMessage = buildResult.AdditionalDetails;
            return buildResult.BuildContext;
        }


    }

    readonly struct SqlCacheKey
    {
        public SqlCacheKey(Expression? expression, IBuildContext? context, EntityColumn? columnDescriptor, SelectQueryClause? selectQuery, ProjectFlags flags)
        {
            Expression = expression;
            Context = context;
            ColumnDescriptor = columnDescriptor;
            SelectQuery = selectQuery;
            Flags = flags;
        }

        public Expression? Expression { get; }
        public IBuildContext? Context { get; }
        public EntityColumn? ColumnDescriptor { get; }
        public SelectQueryClause? SelectQuery { get; }
        public ProjectFlags Flags { get; }

        private sealed class SqlCacheKeyEqualityComparer : IEqualityComparer<SqlCacheKey>
        {
            public bool Equals(SqlCacheKey x, SqlCacheKey y)
            {
                return mooSQL.linq.ExpSameCheckor.Instance.Equals(x.Expression, y.Expression) &&
                       Equals(x.Context, y.Context) &&
                       Equals(x.SelectQuery, y.SelectQuery) &&
                       Equals(x.ColumnDescriptor, y.ColumnDescriptor) &&
                       x.Flags == y.Flags;
            }

            public int GetHashCode(SqlCacheKey obj)
            {
                unchecked
                {
                    var hashCode = (obj.Expression != null ? mooSQL.linq.ExpSameCheckor.Instance.GetHashCode(obj.Expression) : 0);
                    hashCode = (hashCode * 397) ^ (obj.Context != null ? obj.Context.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (obj.SelectQuery != null ? obj.SelectQuery.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (obj.ColumnDescriptor != null ? obj.ColumnDescriptor.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (int)obj.Flags;
                    return hashCode;
                }
            }
        }

        public static IEqualityComparer<SqlCacheKey> SqlCacheKeyComparer { get; } = new SqlCacheKeyEqualityComparer();
    }
    sealed class SubQueryContextInfo
    {
        public Expression SequenceExpression = null!;
        public string? ErrorMessage;
        public IBuildContext? Context;
        public bool IsSequence;
    }

    class SubqueryCacheKey
    {
        public SubqueryCacheKey(SelectQueryClause selectQuery, Expression expression)
        {
            SelectQuery = selectQuery;
            Expression = expression;
        }

        public SelectQueryClause SelectQuery { get; }
        public Expression Expression { get; }

        sealed class BuildContextExpressionEqualityComparer : IEqualityComparer<SubqueryCacheKey>
        {
            public bool Equals(SubqueryCacheKey? x, SubqueryCacheKey? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (ReferenceEquals(x, null))
                {
                    return false;
                }

                if (ReferenceEquals(y, null))
                {
                    return false;
                }

                if (x.GetType() != y.GetType())
                {
                    return false;
                }

                return x.SelectQuery.Equals(y.SelectQuery, ExpressionWord.DefaultComparer) && mooSQL.linq.ExpSameCheckor.Instance.Equals(x.Expression, y.Expression);
            }

            public int GetHashCode(SubqueryCacheKey obj)
            {
                unchecked
                {
                    var hashCode = obj.SelectQuery.SourceID.GetHashCode();
                    hashCode = (hashCode * 397) ^ mooSQL.linq.ExpSameCheckor.Instance.GetHashCode(obj.Expression);
                    return hashCode;
                }
            }
        }

        public static IEqualityComparer<SubqueryCacheKey> Comparer { get; } = new BuildContextExpressionEqualityComparer();
    }

}
