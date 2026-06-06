using System;
using System.Collections.Generic;
using System.Linq;

namespace mooSQL.data
{
    /// <summary>窗口函数 OVER 子句中间表示（Phase F P1）。Extension Token 链迁移目标。</summary>
    public sealed class WindowOverClause
    {
        public IReadOnlyList<string> PartitionExpressions { get; init; } = Array.Empty<string>();
        public IReadOnlyList<WindowOrderItem> OrderItems { get; init; } = Array.Empty<WindowOrderItem>();
        public string? FrameClause { get; init; }

        /// <summary>渲染 OVER 括号内正文（不含 OVER 关键字）。</summary>
        public string RenderBody()
        {
            var parts = new List<string>();
            if (PartitionExpressions.Count > 0)
                parts.Add("PARTITION BY " + string.Join(", ", PartitionExpressions));
            if (OrderItems.Count > 0)
            {
                var order = string.Join(", ", OrderItems.Select(i => i.Render()));
                parts.Add("ORDER BY " + order);
            }
            if (!string.IsNullOrEmpty(FrameClause))
                parts.Add(FrameClause!);
            return string.Join(" ", parts);
        }

        public string RenderWithFunction(string functionSql, SQLExpression expression)
            => expression.windowOver(functionSql, RenderBody());
    }

    /// <summary>ORDER BY 项（含 NULLS FIRST/LAST）。</summary>
    public readonly struct WindowOrderItem
    {
        public string Expression { get; init; }
        public bool Descending { get; init; }
        public string? NullsPosition { get; init; }

        public string Render()
        {
            var s = Descending ? $"{Expression} DESC" : Expression;
            if (NullsPosition != null)
                s += " NULLS " + NullsPosition;
            return s;
        }
    }
}
