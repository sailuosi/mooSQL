namespace mooSQL.data
{
    /// <summary>
    /// SQLBuilder 编排步骤：携带一次 public API 调用的参数，在 Flush 时作用于门面，
    /// 步骤实现通过 <see cref="SQLBuilder.Inner"/> 写入内核，避免构造 API 重入入队。
    /// </summary>
    public interface IStep
    {
        /// <summary>将本步骤应用到编排门面（内部写 <see cref="SQLBuilder.Inner"/>）。</summary>
        void Apply(SQLBuilder builder);

        /// <summary>类型级唯一身份（int，性能优先）。</summary>
        int Id { get; }

        /// <summary>子句家族；Count getter 懒扫时按 Kind 累加/清零。</summary>
        StepKind Kind { get; }

        /// <summary>
        /// 并入编排 Hash：Id、是否产出 SQL(0|1)、结构量；不含参数值内容。
        /// 按 paraRule / opened 判定本步是否产出；Ifsbool 等可改写 opened。
        /// </summary>
        void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened);
    }
}
