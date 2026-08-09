using System.Collections;

namespace mooSQL.data
{
    /// <summary>
    /// whereIn / whereNotIn / whereInGuid 等「key + 集合」步骤基类：
    /// Kind=Where；ContributeHash 按 paraRule 判空写入 0|1，不含元素内容。
    /// </summary>
    public abstract class WhereListStep : StepBase
    {
        public sealed override StepKind Kind { get { return StepKind.Where; } }

        protected readonly string Key;
        private readonly IEnumerable _values;

        protected WhereListStep(string key, IEnumerable values)
        {
            Key = key;
            _values = values;
        }

        public sealed override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            if (!ConsumeOpened(ref opened))
            {
                hc.Add(Id);
                hc.Add(0);
                hc.Add(Key);
                return;
            }

            bool emit;
            if (paraRule == "all")
                emit = true;
            else if (paraRule == "notNull")
                emit = _values != null;
            else
                emit = CollectionHasAny(_values);

            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
            hc.Add(Key);
        }
    }
}
