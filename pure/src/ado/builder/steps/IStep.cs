namespace mooSQL.data
{
    /// <summary>
    /// SQLBuilder 编排步骤：携带一次 public API 调用的参数，在 Flush 时作用于 <see cref="StepBuilder"/>。
    /// </summary>
    public interface IStep
    {
        /// <summary>将本步骤应用到构造宿主。</summary>
        void Apply(StepBuilder builder);
    }
}
