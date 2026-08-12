using System;

namespace mooSQL.data
{
    /// <summary>
    /// Include / includeNav / thenInclude 的安全默认与限流选项。
    /// </summary>
    public sealed class NavIncludeOptions
    {
        /// <summary>全局默认（可在应用启动时改写）。</summary>
        public static NavIncludeOptions Default { get; set; } = new NavIncludeOptions();

        /// <summary>主列表最大行数（IN 键数量上限）。超出抛异常。默认 2000。</summary>
        public int MaxParentCount { get; set; } = 2000;

        /// <summary>单次子查询最大回填行数。超出抛异常。默认 50000；≤0 表示不限制。</summary>
        public int MaxChildRows { get; set; } = 50000;

        /// <summary>thenInclude 最大深度（首层 Include=1）。默认 5。</summary>
        public int MaxDepth { get; set; } = 5;

        /// <summary>
        /// 是否允许主/子任一方启用分片时的 Include。
        /// 默认 false：跨片导航未内建，禁止以免错表。
        /// </summary>
        public bool AllowCrossShard { get; set; } = false;

        /// <summary>浅拷贝一份（链式 thenInclude 共享可变副本时用）。</summary>
        public NavIncludeOptions Clone()
        {
            return new NavIncludeOptions
            {
                MaxParentCount = MaxParentCount,
                MaxChildRows = MaxChildRows,
                MaxDepth = MaxDepth,
                AllowCrossShard = AllowCrossShard
            };
        }
    }
}
