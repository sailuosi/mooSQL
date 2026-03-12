using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 方言/提供程序选项，用于 LINQ 与提供程序行为配置。
    /// </summary>
    public class SooOption
    {


        #region LINQ 部分
        /// <summary>是否预加载分组。</summary>
        public bool PreloadGroups = false;

        /// <summary>是否忽略空更新。</summary>
        public bool IgnoreEmptyUpdate = false;

        /// <summary>是否生成表达式测试。</summary>
        public bool GenerateExpressionTest = false;

        /// <summary>是否跟踪映射器表达式。</summary>
        public bool TraceMapperExpression = false;

        /// <summary>是否不清除 OrderBy。</summary>
        public bool DoNotClearOrderBys = false;

        /// <summary>是否优化连接。</summary>
        public bool OptimizeJoins = true;

        /// <summary>是否将 null 作为值参与比较。</summary>
        public bool CompareNullsAsValues = true;

        /// <summary>是否对分组进行保护。</summary>
        public bool GuardGrouping = true;

        /// <summary>是否禁用查询缓存。</summary>
        public bool DisableQueryCache = false;
        /// <summary>缓存滑动过期时间。</summary>
        public TimeSpan? CacheSlidingExpiration = default;
        /// <summary>是否优先使用 Apply。</summary>
        public bool PreferApply = true;

        /// <summary>是否保持 Distinct 有序。</summary>
        public bool KeepDistinctOrdered = true;

        /// <summary>是否对 Take/Skip 参数化。</summary>
        public bool ParameterizeTakeSkip = true;

        /// <summary>是否启用上下文架构编辑。</summary>
        public bool EnableContextSchemaEdit = false;

        /// <summary>标量是否优先使用 Exists。</summary>
        public bool PreferExistsForScalar = default;
        #endregion

        /// <summary>提供程序能力标志。</summary>
        public SQLProviderFlags ProviderFlags;
    }
}
