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
    public enum EnumParaConvert
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

    /// <summary>
    /// 表/列名大小写策略。None = 未在连接上配置，回退方言 <see cref="SQLExpression.DefaultNameCase"/>。
    /// </summary>
    public enum NameCaseMode
    {
        /// <summary>空白：未指定，使用方言默认。</summary>
        None = 0,
        /// <summary>显式保持映射名大小写，不做 ToUpper/ToLower。</summary>
        Keep = 1,
        /// <summary>转为大写（常见于 Oracle 未加引号约定）。</summary>
        ToUpper = 2,
        /// <summary>转为小写（常见于 PostgreSQL 未加引号约定）。</summary>
        ToLower = 3,
    }

    /// <summary>
    /// 表/列名包裹策略。None = 未在连接上配置，回退方言 <see cref="SQLExpression.DefaultNameQuote"/>。
    /// 引号字符由方言 <see cref="SQLExpression.wrapKeyword"/> 决定。
    /// </summary>
    public enum NameQuoteMode
    {
        /// <summary>空白：未指定，使用方言默认。</summary>
        None = 0,
        /// <summary>凡纯标识符一律加方言引号。</summary>
        Always = 1,
        /// <summary>仅当命中方言保留字时加引号。</summary>
        KeywordsOnly = 2,
        /// <summary>一律不加引号（依赖库的默认折叠规则）。</summary>
        Never = 3,
        /// <summary>
        /// 按需：保留字、非法形态，或 Keep 下含大写且方言会折叠未加引号名时加引号。
        /// </summary>
        IfNeeded = 4,
    }

}

