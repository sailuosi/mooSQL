using mooSQL.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.config
{
    /// <summary>
    /// 可供查询直接使用的，数据库配置信息
    /// </summary>
    public class DBPosition
    {
        /// <summary>
        /// 连接位的索引
        /// </summary>
        public int Position { get; set; }
        /// <summary>
        /// 连接位的名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 数据库类型
        /// </summary>
        public string DbType { get; set; }
        /// <summary>
        /// 连接字符串
        /// </summary>
        public string ConnectString { get; set; }
        /// <summary>
        /// 数据库版本
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 数据库版本号
        /// </summary>
        public double? VersionNumber{ get; set; }
        /// <summary>
        /// 软件版本
        /// </summary>
        public string Edition { get; set; }
        /// <summary>
        /// 软件版本号，数值
        /// </summary>
        public double EditionNumber { get; set; }
        /// <summary>
        /// 是否监控慢SQL，默认关闭。须为属性：Configuration Binder 不绑定字段。
        /// </summary>
        public bool WatchSQL { get; set; } = false;
        /// <summary>
        /// 默认的慢SQL时间阈值，500ms。须为属性：Configuration Binder 不绑定字段。
        /// </summary>
        public int MinTimeSpan { get; set; } = 500;
        /// <summary>
        /// 是否允许读操作，默认 true。须为属性：Configuration Binder 不绑定字段。
        /// </summary>
        public bool Readable { get; set; } = true;
        /// <summary>
        /// 是否允许写操作，默认 true。须为属性：Configuration Binder 不绑定字段。
        /// </summary>
        public bool Writable { get; set; } = true;

        /// <summary>
        /// 自定义探活 SQL，优先级高于方言默认值。
        /// </summary>
        public string CustomPingSQL { get; set; }

        /// <summary>
        /// 探活超时毫秒，映射到 <see cref="DBHealthOptions.PingTimeoutMs"/>。
        /// </summary>
        public int PingTimeoutMs { get; set; }

        /// <summary>
        /// 表/列名大小写策略。默认 <see cref="NameCaseMode.None"/>（未配置，运行时回退方言默认）。
        /// JSON 示例：<c>"ToLower"</c> / <c>"Keep"</c> / <c>"ToUpper"</c>。
        /// </summary>
        public NameCaseMode NameCase { get; set; } = NameCaseMode.None;

        /// <summary>
        /// 表/列名包裹策略。默认 <see cref="NameQuoteMode.None"/>（未配置，运行时回退方言默认）。
        /// JSON 示例：<c>"IfNeeded"</c> / <c>"Always"</c> / <c>"Never"</c>。
        /// </summary>
        public NameQuoteMode NameQuote { get; set; } = NameQuoteMode.None;

        /// <summary>
        /// 枚举写入 ADO 参数时的转换。null = 未配置，回退客户端 <c>SQLBuilderOption.EnumConvert</c>。
        /// JSON 示例：<c>"ToInt"</c> / <c>"ToName"</c> / <c>"None"</c>。
        /// </summary>
        public EnumParaConvert? EnumConvert { get; set; }

        /// <summary>
        /// 查询结果 DataTable 列名策略。null = 未配置，回退方言 <c>DefaultResultColumnName</c>。
        /// JSON 示例：<c>"Pre"</c>（按 select 别名保持大小写）/ <c>"None"</c>（不动）。
        /// </summary>
        public ResultColumnNameMode? ResultColumnName { get; set; }
    }
}
