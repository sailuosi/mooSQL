using System.Linq.Expressions;

namespace mooSQL.data.clip.project
{
    /// <summary>
    /// 从 Select Lambda 收集列根依赖，生成 ProjectionPlan 槽位。
    /// </summary>
    internal static class SelectAnalyzer
    {
        public static ProjectionPlan Analyze(SQLClip clip, LambdaExpression selectLambda)
        {
            var plan = new ProjectionPlan { Source = selectLambda };
            Collect(clip, selectLambda.Body, plan);
            return plan;
        }

        private static void Collect(SQLClip clip, Expression node, ProjectionPlan plan)
        {
            if (node == null)
                return;

            if (ColumnRootResolver.TryResolve(clip, node, out var root))
            {
                plan.GetOrAddSlot(root);
                return;
            }

            switch (node)
            {
                case NewExpression neu:
                    foreach (var arg in neu.Arguments)
                        Collect(clip, arg, plan);
                    break;
                case MemberInitExpression init:
                    foreach (var b in init.Bindings)
                    {
                        if (b is MemberAssignment ass)
                            Collect(clip, ass.Expression, plan);
                    }
                    break;
                case MemberExpression mem:
                    Collect(clip, mem.Expression, plan);
                    break;
                case MethodCallExpression call:
                    if (call.Object != null)
                        Collect(clip, call.Object, plan);
                    foreach (var arg in call.Arguments)
                        Collect(clip, arg, plan);
                    break;
                case BinaryExpression bin:
                    Collect(clip, bin.Left, plan);
                    Collect(clip, bin.Right, plan);
                    break;
                case ConditionalExpression cond:
                    Collect(clip, cond.Test, plan);
                    Collect(clip, cond.IfTrue, plan);
                    Collect(clip, cond.IfFalse, plan);
                    break;
                case UnaryExpression uni:
                    Collect(clip, uni.Operand, plan);
                    break;
                case NewArrayExpression arr:
                    foreach (var e in arr.Expressions)
                        Collect(clip, e, plan);
                    break;
                case InvocationExpression inv:
                    Collect(clip, inv.Expression, plan);
                    foreach (var a in inv.Arguments)
                        Collect(clip, a, plan);
                    break;
            }
        }
    }
}
