using System;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// 无脏字段时的行为。
    /// </summary>
    public enum DirtyEmptyBehavior
    {
        /// <summary>不执行更新，返回 0 / false。</summary>
        NoOp = 0,
        /// <summary>抛出异常。</summary>
        Throw = 1,
        /// <summary>回退为全列更新。</summary>
        FallBackAllColumns = 2
    }

    /// <summary>
    /// 脏字段追踪选项。
    /// </summary>
    public sealed class TrackingOptions
    {
        /// <summary>
        /// 永不参与脏检测的属性名（BLOB/JSON/审计列等）。
        /// </summary>
        public HashSet<string> ExcludeMembers { get; } = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// 默认排除 byte[] / Stream 等大列。
        /// </summary>
        public bool ExcludeLargeTypes { get; set; } = true;

        /// <summary>
        /// 无脏字段时的行为（默认 NoOp）。
        /// </summary>
        public DirtyEmptyBehavior EmptyBehavior { get; set; } = DirtyEmptyBehavior.NoOp;

        /// <summary>
        /// 未追踪实体调用 Update 时：true 则全列（兼容）；false 则按 EmptyBehavior。
        /// </summary>
        public bool UntrackedUpdateAllColumns { get; set; } = true;
    }
}
