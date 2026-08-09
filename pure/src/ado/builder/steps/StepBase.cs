using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// IStep 默认基类：Id / Kind / HasSql(0|1) 进入编排 Hash。
    /// </summary>
    public abstract class StepBase : IStep
    {
        /// <inheritdoc />
        public abstract int Id { get; }

        /// <inheritdoc />
        public abstract StepKind Kind { get; }

        /// <summary>本步是否产出 SQL 文本（编排期可判定）。默认 true。</summary>
        protected virtual bool HasSql
        {
            get { return true; }
        }

        /// <inheritdoc />
        public abstract void Apply(SQLBuilder builder);

        /// <inheritdoc />
        public virtual void ContributeHash(ref ScriptHash hc)
        {
            hc.Add(Id);
            hc.Add(HasSql ? 1 : 0);
            ContributeStructuralHash(ref hc);
        }

        /// <summary>追加编排结构量（列名/op/paramed 等）；不含参数值内容。</summary>
        protected virtual void ContributeStructuralHash(ref ScriptHash hc)
        {
        }

        /// <summary>子步骤磁带：ChildBegin / 各步 ContributeHash / ChildEnd。</summary>
        protected static void ContributeChildSteps(ref ScriptHash hc, IReadOnlyList<IStep> children)
        {
            hc.Add(StepHashMarks.ChildBegin);
            if (children != null)
            {
                for (int i = 0; i < children.Count; i++)
                {
                    if (children[i] != null)
                        children[i].ContributeHash(ref hc);
                }
            }
            hc.Add(StepHashMarks.ChildEnd);
        }
    }
}
