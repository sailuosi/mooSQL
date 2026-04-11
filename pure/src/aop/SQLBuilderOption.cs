// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// <see cref="SQLBuilder"/> 的可选行为配置（如 UPDATE SET 时 NULL 的处理策略）。
    /// </summary>
    public class SQLBuilderOption
    {

        /// <summary>
        /// UPDATE SET 时是否写入 NULL 等策略。
        /// </summary>
        public UpdateSetNullOption UpdateSetNullOpt=UpdateSetNullOption.None;
    }
}