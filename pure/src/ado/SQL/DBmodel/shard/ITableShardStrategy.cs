using System;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// 分表路由策略：单点解析与范围枚举。
    /// </summary>
    public interface ITableShardStrategy
    {
        string ResolvePoint(EntityInfo en, object rowOrNull, DateTime? pointTime);

        IReadOnlyList<string> ResolveRange(EntityInfo en, DateTime from, DateTime to);

        /// <summary>枚举实体已配置规则下的全部分表名（至当前时间），用于 DDL 同步等。</summary>
        IReadOnlyList<string> ResolveAllTables(EntityInfo en);
    }
}
