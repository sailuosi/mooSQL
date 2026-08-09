using System;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// B 类子查询：编排期捕获子步骤队列；Apply 时 getBrotherBuilder + 重放（不调用委托重载）。
    /// </summary>
    public partial class SQLBuilder
    {
        /// <summary>编排期执行委托，收集其入队的步骤快照。</summary>
        internal static List<IStep> CaptureChildSteps(Action<SQLBuilder> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var child = new SQLBuilder();
            action(child);
            return child.CopySteps();
        }

        /// <summary>复制当前门面已入队步骤。</summary>
        internal List<IStep> CopySteps()
        {
            return new List<IStep>(_steps);
        }

        /// <summary>
        /// 将已摊平的子步骤重放到内核宿主（须已由 <see cref="StepBuilder.getBrotherBuilder"/> 创建）。
        /// Attach 仅作 <see cref="IStep.Apply(SQLBuilder)"/> 适配，不替代 getBrotherBuilder。
        /// </summary>
        internal static void ReplaySteps(StepBuilder host, IReadOnlyList<IStep> steps)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));
            if (steps == null || steps.Count == 0)
                return;

            var facade = Attach(host, materializing: true);
            for (int i = 0; i < steps.Count; i++)
            {
                steps[i].Apply(facade);
            }
        }
    }
}
