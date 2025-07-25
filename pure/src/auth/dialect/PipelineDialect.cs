// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.auth
{
    /// <summary>
    /// 角色权限解析过程中的切面逻辑
    /// </summary>
    public abstract class PipelineDialect
    {
        /// <summary>
        /// 将角色数据范围定义写入到数据范围池。
        /// </summary>
        /// <param name="range"></param>
        /// <param name="role"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public abstract int readRoleScopeCode(WordBagDialect range, AuthWord role, AuthUser user);
        /// <summary>
        /// 解析角色的语义
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public virtual ConditionGroup parseAuthWord(AuthWord role) {
            return null;
        }
        /// <summary>
        /// 读取语义参数
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="parameters"></param>
        public virtual void readAuthWordPara(Condition condition,Dictionary<string, Object> parameters)
        {
            if (!string.IsNullOrWhiteSpace(condition.Value) && parameters.ContainsKey(condition.Value)) {
                condition.Value = parameters[condition.Value].ToString();
            }
        }
    }
}