namespace mooSQL.data
{
    /// <summary>壳内已定名的静态参数槽；值每次请求重绑。</summary>
    public struct StaticSlot
    {
        /// <summary>编排期 StaticSlotId。</summary>
        public int SlotId;

        /// <summary>写入壳的物理名（不含方言前缀，如 ms_s0）。</summary>
        public string NameInTemplate;
    }
}
