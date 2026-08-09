namespace mooSQL.data
{
    /// <summary>
    /// ScriptTemplate 在 <see cref="ISooCache"/> 中的键（字符串形式，便于与 HashCache 共存）。
    /// </summary>
    public static class ScriptCacheKey
    {
        /// <summary>键前缀，与结果缓存用户键隔离。</summary>
        public const string Prefix = "moo.st:";

        /// <summary>Live PlaceHolder 格式版本（与 <see cref="LiveParaMarks"/> 对齐）。</summary>
        public const string LivePlaceHolderSchemaVersion = "1";

        /// <summary>方言表达式版本占位；C4 再接真实 VersionNumber。</summary>
        public const string ExpressionVersionPlaceholder = "0";

        public const string BuildKindToSelect = "ToSelect";

        /// <summary>
        /// 复合键：编排 Hash + 库类型 + 出口 + 命名/占位版本 + seed。
        /// </summary>
        public static string Format(
            int orchestrationHash,
            DataBaseType dbType,
            string buildKind,
            string paraSeed)
        {
            return Prefix
                + orchestrationHash.ToString("X8")
                + ":"
                + ((int)dbType).ToString()
                + ":"
                + (buildKind ?? "")
                + ":"
                + ExpressionVersionPlaceholder
                + ":"
                + StaticSlotMarks.NameSchemaVersion
                + ":"
                + LivePlaceHolderSchemaVersion
                + ":"
                + (paraSeed ?? "");
        }
    }
}
