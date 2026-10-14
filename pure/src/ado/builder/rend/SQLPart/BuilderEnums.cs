// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 编织器枚举
    /// </summary>
    public class BuilderEnums
    {
    }
    /// <summary>
    /// 设置update语句中的 set策略
    /// </summary>
    public enum UpdateSetNullOption
    {
        /// <summary>
        /// 表示未配置，执行默认操作，优先级最低会被其它覆盖
        /// </summary>
        None=0,
        /// <summary>
        /// 当值为null时忽略
        /// </summary>
        IgnoreNull=1,
        /// <summary>
        /// 设置为数据库下的 null 
        /// </summary>
        AsDBNull=2


    }

    /// <summary>
    /// CLR 枚举写入 ADO 参数时的转换策略（仅绑定阶段生效）。
    /// </summary>
    public enum EnumParameterConvert
    {
        /// <summary>
        /// 不转换，原样写入（默认，兼容现状）。
        /// </summary>
        None = 0,
        /// <summary>
        /// 转为底层整型（int/long/…）。
        /// </summary>
        ToInt = 1,
        /// <summary>
        /// 转为枚举名称（ToString）。
        /// </summary>
        ToName = 2,
        /// <summary>
        /// 转为数字的字符串形式（如 "2"）。
        /// </summary>
        ToIntString = 3,
    }

}

