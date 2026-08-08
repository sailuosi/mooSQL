namespace mooSQL.data
{
    /// <summary>
    /// SQL 构建碎片，保存可重放的 API 步骤脚本，供 <see cref="StepBuilder.useApart"/> 复用。
    /// 可通过 <see cref="StepBuilder.toApart"/> 快照当前 Builder，或通过 <see cref="StepBuilder.record"/>/<see cref="StepBuilder.stop"/> 录播独立片段。
    /// 一阶段仅支持同类 <see cref="DataBaseType"/> 复用；二阶段预留跨库 <c>convertApart</c>（未实现）。
    /// </summary>
    public sealed class SQLApart
    {
        internal ApartBuildScript Script { get; }
        internal DataBaseType SourceDbType { get; }

        internal SQLApart(ApartBuildScript script, DataBaseType sourceDbType)
        {
            Script = script;
            SourceDbType = sourceDbType;
        }

        /// <summary>
        /// 清空碎片内步骤，后续 useApart 不再带入内容。
        /// </summary>
        public void clear()
        {
            Script.Clear();
        }
    }
}
