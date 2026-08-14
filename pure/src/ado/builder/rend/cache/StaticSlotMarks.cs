using System;

namespace mooSQL.data
{
    /// <summary>
    /// 编排期静态槽位命名（方案 C）。物理名不含方言前缀；WhereFrag.ToSQL 会加 paraPrefix。
    /// NameSchemaVersion=2：纳入 paraSeed（含兄弟 lvN_）与 group seed，对齐经典 where/set 起名。
    /// </summary>
    public static class StaticSlotMarks
    {
        /// <summary>写入 ScriptCacheKey 的命名模式版本。</summary>
        public const string NameSchemaVersion = "2";

        /// <summary>
        /// where 槽位名：对齐经典 <c>k{paraSeed}g{paramPrefix}wp{N}</c>，以 <c>ms_s{slotId}</c> 替换 wp 计数。
        /// </summary>
        public static string FormatWhereName(string paraSeed, string groupParamPrefix, int slotId)
        {
            return string.Format("k{0}g{1}ms_s{2}", paraSeed ?? "", groupParamPrefix ?? "", slotId);
        }

        /// <summary>
        /// set 槽位名：对齐经典 <c>{paraSeed}cl_{groupKey}_…</c>，以 <c>ms_s{slotId}</c> 替换 field/set 计数。
        /// </summary>
        public static string FormatSetName(string paraSeed, string groupKey, int slotId)
        {
            return string.Format("{0}cl_{1}_ms_s{2}", paraSeed ?? "", groupKey ?? "", slotId);
        }

        /// <summary>旧裸名（无 seed）；仅兼容，勿用于新写参。</summary>
        [Obsolete("Use FormatWhereName / FormatSetName (NameSchemaVersion=2).")]
        public static string FormatName(int slotId)
        {
            return FormatWhereName("", "", slotId);
        }
    }
}
