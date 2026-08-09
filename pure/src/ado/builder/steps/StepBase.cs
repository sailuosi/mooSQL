using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// IStep 基类：Id / Kind / Apply；各步直接 override ContributeHash。
    /// </summary>
    public abstract class StepBase : IStep
    {
        /// <inheritdoc />
        public abstract int Id { get; }

        /// <inheritdoc />
        public abstract StepKind Kind { get; }

        /// <inheritdoc />
        public abstract void Apply(SQLBuilder builder);

        /// <inheritdoc />
        public abstract void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened);

        /// <summary>子步骤磁带：ChildBegin / 各步 ContributeHash / ChildEnd。</summary>
        protected static void ContributeChildSteps(ref ScriptHash hc, IReadOnlyList<IStep> children, string paraRule)
        {
            hc.Add(StepHashMarks.ChildBegin);
            if (children != null)
            {
                var childOpened = true;
                for (int i = 0; i < children.Count; i++)
                {
                    if (children[i] != null)
                        children[i].ContributeHash(ref hc, paraRule, ref childOpened);
                }
            }
            hc.Add(StepHashMarks.ChildEnd);
        }

        /// <summary>与 StepBuilder.paraRule 同语义：notEmpty / notNull / all。</summary>
        protected static bool PassesParaRule(string paraRule, object val)
        {
            if (paraRule == "all")
                return true;
            if (paraRule == "notNull")
                return val != null;
            if (val == null)
                return false;
            var s = val.ToString();
            return s != null && !string.IsNullOrEmpty(s.Trim());
        }

        /// <summary>集合是否至少有一个元素（whereIn 等）。</summary>
        protected static bool CollectionHasAny(IEnumerable items)
        {
            if (items == null)
                return false;
            var it = items.GetEnumerator();
            try
            {
                return it.MoveNext();
            }
            finally
            {
                var d = it as System.IDisposable;
                if (d != null)
                    d.Dispose();
            }
        }

        /// <summary>ifs 门控：若关闭则消费并返回 false（本步不落 SQL）。</summary>
        protected static bool ConsumeOpened(ref bool opened)
        {
            if (!opened)
            {
                opened = true;
                return false;
            }
            return true;
        }
    }
}
