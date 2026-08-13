using System;
using System.Linq.Expressions;
using System.Reflection;

namespace mooSQL.data.clip.project
{
    /// <summary>
    /// 将 Select Lambda 中的列根改写为 RowBag.Get&lt;T&gt;(slot)，并 Compile。
    /// </summary>
    internal static class ClientProjectorCompiler
    {
        private static readonly MethodInfo GetMethod =
            typeof(RowBag).GetMethod(nameof(RowBag.Get), BindingFlags.Instance | BindingFlags.Public);

        public static Delegate Compile(SQLClip clip, ProjectionPlan plan, bool nullPropagateTailCalls)
        {
            var resultType = plan.Source.ReturnType;
            var rowParam = Expression.Parameter(typeof(RowBag), "row");
            var rewriter = new Rewriter(clip, plan, rowParam, nullPropagateTailCalls);
            var body = rewriter.Visit(plan.Source.Body);
            var lambdaType = typeof(Func<,>).MakeGenericType(typeof(RowBag), resultType);
            var lambda = Expression.Lambda(lambdaType, body, rowParam);
            return lambda.Compile();
        }

        private sealed class Rewriter : ExpressionVisitor
        {
            private readonly SQLClip _clip;
            private readonly ProjectionPlan _plan;
            private readonly ParameterExpression _row;
            private readonly bool _nullPropagate;

            public Rewriter(SQLClip clip, ProjectionPlan plan, ParameterExpression row, bool nullPropagate)
            {
                _clip = clip;
                _plan = plan;
                _row = row;
                _nullPropagate = nullPropagate;
            }

            protected override Expression VisitUnary(UnaryExpression node)
            {
                if (node.NodeType == ExpressionType.Convert ||
                    node.NodeType == ExpressionType.ConvertChecked ||
                    node.NodeType == ExpressionType.TypeAs)
                {
                    var op = Visit(node.Operand);
                    if (op == node.Operand)
                        return node;
                    if (op.Type == node.Type)
                        return op;
                    return Expression.MakeUnary(node.NodeType, op, node.Type, node.Method);
                }

                return base.VisitUnary(node);
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                if (ColumnRootResolver.TryResolve(_clip, node, out var root))
                    return MakeGet(root, node.Type);

                var visited = base.VisitMember(node);
                if (_nullPropagate && visited is MemberExpression mem && mem.Expression != null)
                    return NullSafeInstanceAccess(mem.Expression, mem, node.Type);

                return visited;
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (!_nullPropagate || node.Object == null)
                    return base.VisitMethodCall(node);

                var instance = Visit(node.Object);
                Expression[] args = new Expression[node.Arguments.Count];
                for (int i = 0; i < node.Arguments.Count; i++)
                    args[i] = Visit(node.Arguments[i]);

                var call = node.Object == instance && ArgumentsSame(node.Arguments, args)
                    ? node
                    : Expression.Call(instance, node.Method, args);

                return NullSafeInstanceAccess(instance, call, node.Type);
            }

            private static bool ArgumentsSame(System.Collections.ObjectModel.ReadOnlyCollection<Expression> a, Expression[] b)
            {
                if (a.Count != b.Length)
                    return false;
                for (int i = 0; i < b.Length; i++)
                {
                    if (!ReferenceEquals(a[i], b[i]))
                        return false;
                }
                return true;
            }

            private Expression NullSafeInstanceAccess(Expression instance, Expression access, Type resultType)
            {
                if (instance == null || !IsMaybeNullReference(instance.Type))
                    return access;

                var lifted = LiftToNullable(resultType);
                var ifFalse = access.Type == lifted
                    ? access
                    : Expression.Convert(access, lifted);
                return Expression.Condition(
                    Expression.Equal(instance, Expression.Constant(null, instance.Type)),
                    Expression.Default(lifted),
                    ifFalse);
            }

            private static bool IsMaybeNullReference(Type type)
            {
                if (!type.IsValueType)
                    return true;
                return Nullable.GetUnderlyingType(type) != null;
            }

            private static Type LiftToNullable(Type type)
            {
                if (!type.IsValueType)
                    return type;
                if (Nullable.GetUnderlyingType(type) != null)
                    return type;
                return typeof(Nullable<>).MakeGenericType(type);
            }

            private Expression MakeGet(ColumnRoot root, Type resultType)
            {
                if (!_plan.SlotsByKey.TryGetValue(root.Key, out var slot))
                    slot = _plan.GetOrAddSlot(root);

                var get = GetMethod.MakeGenericMethod(resultType);
                return Expression.Call(_row, get, Expression.Constant(slot.Index));
            }
        }
    }
}
