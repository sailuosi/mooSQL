using System.Collections;

namespace mooSQL.data
{
    /// <summary>
    /// whereIn / whereNotIn / whereInGuid 等「key + 集合」步骤基类：
    /// Kind=Where；null 集合 HasSql=0；非 null（含空集合）HasSql=1，并以空/非空形状位区分（P5）。
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

        /// <summary>集合引用（供子类 Apply 判定 null）。</summary>
        protected IEnumerable Values { get { return _values; } }

        public sealed override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            if (!ConsumeOpened(ref opened))
            {
                hc.Add(Id);
                hc.Add(0);
                hc.Add(Key);
                return;
            }

            // null：忽略（无 SQL）；空集合仍有 SQL（IN→1=2 等），HasSql=1
            if (_values == null && paraRule != "all")
            {
                hc.Add(Id);
                hc.Add(0);
                hc.Add(Key);
                return;
            }

            hc.Add(Id);
            hc.Add(1);
            hc.Add(Key);
            // 结构形状：空 vs 非空（不 Combine 元素内容）
            hc.Add(CollectionHasAny(_values) ? 1 : 0);
        }
    }
}
