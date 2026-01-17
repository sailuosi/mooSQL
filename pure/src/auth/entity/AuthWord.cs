// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.auth
{
    /// <summary>
    /// 角色数据权限范围码集合。
    /// </summary>
    public class AuthWord
    {
        /// <summary>
        /// 主键
        /// </summary>
        public string id;
        /// <summary>
        /// 数据范围编码
        /// </summary>
        public string scopeCode;
        /// <summary>
        /// 编号
        /// </summary>
        public string code;
        /// <summary>
        /// 名称
        /// </summary>
        public string name;
        /// <summary>
        /// 解释器
        /// </summary>
        public string parser;
        /// <summary>
        /// 参数
        /// </summary>
        public string para;
        /// <summary>
        /// 类型
        /// </summary>
        public string type;
        /// <summary>
        /// 角色ID
        /// </summary>
        public string roleId;
        /// <summary>
        /// 分组ID，同一分组的条件之间，使用and连接
        /// </summary>
        public string groupId;
    }
}
