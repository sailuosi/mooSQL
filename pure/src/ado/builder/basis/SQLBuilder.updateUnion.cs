using System.Collections.Generic;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 多行值源 UPDATE（UpdateUnion）：按行 SetFrag 自动分桶后拼 UPDATE + UNION。
    /// </summary>
    public abstract partial class SQLBuilder
    {
        /// <summary>
        /// 从当前多行 <c>set</c>/<c>newRow</c> 状态构建 UpdateUnion 命令。
        /// 按各行实际 SET 列签名自动分桶；异构时一条 <see cref="SQLCmd"/> 内多段；
        /// 单行桶退化为普通 UPDATE。方言不支持且存在多行同构桶时返回空命令。
        /// </summary>
        /// <param name="keyColumns">ON / WHERE 键列；其余有值列为 SET。</param>
        public abstract SQLCmd toUpdateUnion(IReadOnlyList<string> keyColumns);

        /// <summary>
        /// 执行 <see cref="toUpdateUnion"/>；空命令返回 -1。影响行数为各段之和。
        /// </summary>
        public abstract int doUpdateUnion(IReadOnlyList<string> keyColumns);

        /// <summary>
        /// 异步执行 <see cref="toUpdateUnion"/>。
        /// </summary>
        public abstract Task<int> doUpdateUnionAsync(IReadOnlyList<string> keyColumns);
    }
}
