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
    }
}
