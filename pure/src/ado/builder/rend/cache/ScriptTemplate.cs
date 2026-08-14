namespace mooSQL.data
{
    /// <summary>
    /// 可缓存的执行模板（未 Resolve 的壳 + 静态槽表 + Live 个数）。
    /// 存入 <see cref="ISooCache"/>（与结果缓存共用 Client.Cache / setCacheHolder）。
    /// </summary>
    public sealed class ScriptTemplate
    {
        /// <summary>未 ResolveDelayParas 的 SQL 文本。</summary>
        public string ShellSql;

        /// <summary>有序静态槽；桥 = SlotId → NameInTemplate。</summary>
        public StaticSlot[] StaticSlots;

        /// <summary>壳内 Live PlaceHolder 个数。</summary>
        public int LiveCount;

        /// <summary>冷路径时的 paraSeed（纳入 Key；此处存证）。</summary>
        public string ParaSeed;

        /// <summary>编排指纹存证（可选校验）。</summary>
        public int OrchestrationHash;
    }
}
