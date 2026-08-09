namespace mooSQL.data
{
    /// <summary>
    /// 编排期静态槽位命名（方案 C）。物理名不含方言前缀；WhereFrag.ToSQL 会加 paraPrefix。
    /// </summary>
    public static class StaticSlotMarks
    {
        /// <summary>写入 ScriptCacheKey 的命名模式版本。</summary>
        public const string NameSchemaVersion = "1";

        /// <summary>派生参数键：ms_s{slotId}。</summary>
        public static string FormatName(int slotId)
        {
            return "ms_s" + slotId;
        }
    }
}
