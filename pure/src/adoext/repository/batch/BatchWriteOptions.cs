namespace mooSQL.data
{
    /// <summary>仓储批量写入模式。</summary>
    public enum BatchWriteMode
    {
        /// <summary>按阈值自动选择逐行 / 多行 VALUES / Bulk。</summary>
        Auto = 0,
        /// <summary>强制逐行。</summary>
        Row = 1,
        /// <summary>强制多行 VALUES（SQLBuilder）。</summary>
        MultiValues = 2,
        /// <summary>强制 Bulk。</summary>
        Bulk = 3,
    }

    /// <summary>
    /// <see cref="SooRepository{T}"/> 批量写入阈值与模式（本轮挂在 Repo，不挂 Client）。
    /// </summary>
    public class BatchWriteOptions
    {
        /// <summary>写入模式，默认 Auto。</summary>
        public BatchWriteMode Mode { get; set; } = BatchWriteMode.Auto;

        /// <summary>低于此行数走逐行插入。默认 32。</summary>
        public int InsertRowThreshold { get; set; } = 32;

        /// <summary>达到此行数优先 Bulk。默认 2000。</summary>
        public int InsertBulkThreshold { get; set; } = 2000;

        /// <summary>多行 VALUES 建议批大小（交给 SQLBuilder.InsertValuesBatchSize）。默认 200。</summary>
        public int InsertBatchSize { get; set; } = 200;

        /// <summary>低于此行数走逐行更新。默认 32。</summary>
        public int UpdateRowThreshold { get; set; } = 32;

        /// <summary>更新多语句每片条数。默认 100。</summary>
        public int UpdateBatchSize { get; set; } = 100;
    }
}
