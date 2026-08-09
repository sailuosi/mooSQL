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

        /// <summary>子句家族；门面按此更新计数。</summary>
        StepKind Kind { get; }

        /// <summary>
        /// 并入编排 Hash：须包含 Id、HasSql(0|1)、编排结构量；不含参数值内容。
        /// </summary>
        void ContributeHash(ref ScriptHash hc);
    }
}
