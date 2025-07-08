using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.slave
{
    /// <summary>
    /// 变更中介，负责接待变更指令，分发后续事宜
    /// </summary>
    public class ModifyMediator
    {

        /// <summary>
        /// 已注册的监听器
        /// </summary>
        public List<IModifyListener> modifyEars = new List<IModifyListener>();


        /// <summary>
        /// 暴露给触发器的方法
        /// </summary>
        /// <param name="e"></param>
        public void emitModify(ModifyPara e) {
            foreach (var listener in modifyEars) {
                listener.ListenTo(e);
            }
            
        }

        /// <summary>
        /// 注册监听器
        /// </summary>
        /// <param name="listener"></param>
        public void signModify(IModifyListener listener) { 
            if(!modifyEars.Contains(listener)) { modifyEars.Add(listener); }
        }

    }
}
