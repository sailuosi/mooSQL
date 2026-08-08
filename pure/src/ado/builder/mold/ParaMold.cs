using System;

namespace mooSQL.data
{
    /// <summary>
    /// SQLMold 形参档案（与 <see cref="SQLBuilder.paraSeed"/> 参数名前缀字符串无关）。
    /// MaskBits：0=跳过，1=标量纳入，2=列表，3=Format。
    /// </summary>
    public sealed class ParaMold
    {
        /// <summary>L1 遇见形参的次序（稳定槽位序）。</summary>
        public int VisitId { get; set; }

        /// <summary>接点位掩码（并入 PathKey）。</summary>
        public int MaskBits { get; set; }

        /// <summary>运行时值；列表/Format 持有源数据。</summary>
        public object Value { get; set; }

        /// <summary>
        /// 模版中的占位标记。标量为方言前缀+参数名；列表/Format 为 <c>{{mN}}</c>。
        /// </summary>
        public string Placeholder { get; set; }

        /// <summary>参数名（不含方言前缀），标量 L2 直接入 Paras。</summary>
        public string ParamName { get; set; }

        /// <summary>是否为列表槽（whereIn 等）。</summary>
        public bool IsList { get; set; }

        /// <summary>是否为 Format 槽。</summary>
        public bool IsFormat { get; set; }

        /// <summary>展开处理器；列表/Format 必填；标量可为 null。</summary>
        public Func<ParaMold, MoldExpandResult> Processor { get; set; }

        /// <summary>字段名 / Format kind（诊断/掩码）。</summary>
        public string Field { get; set; }

        /// <summary>操作符；Format 时可为模板串。</summary>
        public string Op { get; set; }

        /// <summary>IN 元素个数（列表专用）。</summary>
        public int Arity { get; set; }

        /// <summary>是否已 Commit（MaskBits 已定）。</summary>
        public bool Committed { get; set; }
    }
}
