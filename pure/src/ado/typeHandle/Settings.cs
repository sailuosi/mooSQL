using System;
using System.Data;
using System.Threading;

namespace mooSQL.data
{

    /// <summary>
    /// 允许全局指定某些 SqlMapper 值。
    /// </summary>
    public static class Settings
    {
        // disable single result by default; prevents errors AFTER the select being detected properly
        private const CommandBehavior DefaultAllowedCommandBehaviors = ~CommandBehavior.SingleResult;
        internal static CommandBehavior AllowedCommandBehaviors { get; private set; } = DefaultAllowedCommandBehaviors;



        /// <summary>
        /// 指示数据中的 null 值是否被静默忽略（默认）还是主动应用并分配给成员
        /// </summary>
        public static bool ApplyNullValues { get; set; }



        /// <summary>
        /// 如果设置，伪位置参数（即 ?foo?）将使用自动生成的增量名称传递，即 "1"、"2"、"3"，
        /// 而不是原始名称；对于大多数场景，这会被忽略，因为名称是冗余的，但 "snowflake" 需要此设置。
        /// </summary>
        public static bool UseIncrementalPseudoPositionalParameterNames { get; set; }


    }
    
}
