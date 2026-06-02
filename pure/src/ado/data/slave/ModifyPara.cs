using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.slave
{
    /// <summary>
    /// 修改语句参数体
    /// </summary>
    public class ModifyPara
    {

        /// <summary>
        /// 归属的主库索引
        /// </summary>
        public int position;
        /// <summary>
        /// 从库实例
        /// </summary>
        public DBInstance DB;
        /// <summary>
        /// 待执行的命令
        /// </summary>
        public SQLCmd cmd;
        /// <summary>
        /// 事件类型
        /// </summary>
        public ModifyEventType type;

    }

    /// <summary>
    /// 枚举 ModifyEventType。
    /// </summary>
    public enum ModifyEventType
    {

        /// <summary>
        /// 内部成员说明。
        /// </summary>
        Modify = 0,
    }
}