using System.Linq.Expressions;

namespace mooSQL.data.clip.project
{
    /// <summary>
    /// 廉价探测：Select 是否需要客户端尾投影。纯列早退，避免完整 Analyze。
    /// </summary>
    internal static class SelectClientTailProbe
    {
        public static bool NeedsClientTail(SQLClip clip, Expression body)
        {
            body = ColumnRootResolver.UnwrapConvert(body);
            // 仅 New / MemberInit 可能含尾投影；整表引用、单列等交旧路径
            if (body is not NewExpression && body is not MemberInitExpression)
                return false;
            return !IsPureColumnTree(clip, body);
        }

        private static bool IsPureColumnTree(SQLClip clip, Expression node)
        {
            if (node == null)
                return true;

            node = ColumnRootResolver.UnwrapConvert(node);

            switch (node)
            {
                case NewExpression neu:
                    for (int i = 0; i < neu.Arguments.Count; i++)
                    {
                        if (!IsPureColumnTree(clip, neu.Arguments[i]))
                            return false;
                    }
                    return true;

                case MemberInitExpression init:
                    foreach (var b in init.Bindings)
                    {
                        if (b is MemberAssignment ass && !IsPureColumnTree(clip, ass.Expression))
                            return false;
                    }
                    return true;

                case MemberExpression:
                    return ColumnRootResolver.TryResolve(clip, node, out _);

                default:
                    // MethodCall / Conditional / Binary / Constant…
                    return false;
            }
        }
    }
}
