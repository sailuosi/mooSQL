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

        /// <summary>
        /// 枚举值写入数据库命令参数时的转换策略（仅 <c>Paras</c>→ADO 绑定）。
        /// </summary>
        public EnumParaConvert EnumConvert = EnumParaConvert.None;
    }
}