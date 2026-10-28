using System.Collections.Generic;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 多行值源批量 UPDATE（UpdateBatch）：按行 SetFrag 自动分桶后由方言渲染（UNION / CTE 等）。
    /// </summary>
    public abstract partial class SQLBuilder
    {
        /// <summary>
        /// 从当前多行 <c>set</c>/<c>newRow</c> 状态构建 UpdateBatch 命令。
        /// 按各行实际 SET 列签名自动分桶；异构时一条 <see cref="SQLCmd"/> 内多段；
        /// 单行桶退化为普通 UPDATE。方言不支持且存在多行同构桶时返回空命令。
        /// </summary>
        /// <param name="keyColumns">ON / WHERE 键列；其余有值列为 SET。</param>
        public abstract SQLCmd toUpdateBatch(IReadOnlyList<string> keyColumns);

        /// <summary>
        /// 执行 <see cref="toUpdateBatch"/>；空命令返回 -1。影响行数为各段之和。
        /// </summary>
        public abstract int doUpdateBatch(IReadOnlyList<string> keyColumns);

        /// <summary>
        /// 异步执行 <see cref="toUpdateBatch"/>。
        /// </summary>
        public abstract Task<int> doUpdateBatchAsync(IReadOnlyList<string> keyColumns);
    }
}
