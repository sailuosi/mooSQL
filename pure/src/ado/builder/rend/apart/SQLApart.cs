using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// SQL 构建碎片：保存编排步骤磁带（<see cref="IStep"/>），供 <see cref="SQLBuilder.useApart"/> 重放入队。
    /// 可通过 <see cref="SQLBuilder.toApart"/> 快照当前编排，或通过 <see cref="SQLBuilder.record"/>/<see cref="SQLBuilder.stop"/> 截取片段。
    /// 一阶段仅支持同类 <see cref="DataBaseType"/> 复用。
    /// </summary>
    public sealed class SQLApart
    {
        internal List<IStep> Steps { get; }
        internal DataBaseType SourceDbType { get; }

        internal SQLApart(List<IStep> steps, DataBaseType sourceDbType)
        {
            Steps = steps ?? new List<IStep>();
            SourceDbType = sourceDbType;
        }

        /// <summary>
        /// 清空碎片内步骤，后续 useApart 不再带入内容。
        /// </summary>
        public void clear()
        {
            Steps.Clear();
        }
    }
}
