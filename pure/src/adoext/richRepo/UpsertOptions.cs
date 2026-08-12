using System.Collections.Generic;

namespace mooSQL.data.richRepo
{
    /// <summary>
    /// Repo / RichRepo 级 Upsert 选项（对标 CRL InsertOrUpdateOption）。
    /// </summary>
    public sealed class UpsertOptions
    {
        /// <summary>唯一判定列；空则用主键。</summary>
        public List<string> ConstraintMembers { get; } = new List<string>();

        /// <summary>匹配后要更新的列；空则更新除约束列外全部可更新列。</summary>
        public List<string> UpdateMembers { get; } = new List<string>();

        /// <summary>存在则跳过更新（仅插入）。</summary>
        public bool IfExistsSkipUpdate { get; set; }

        /// <summary>
        /// 批量时逐条 InsertOrUpdate 的切片大小（非多行 SQL）。
        /// 默认 500；≤0 表示整批仍逐条、不再二次切片。
        /// </summary>
        public int BatchSize { get; set; } = 500;

        /// <summary>调试：最后生成相关说明或 SQL。</summary>
        public string SqlOut { get; set; }
    }
}
