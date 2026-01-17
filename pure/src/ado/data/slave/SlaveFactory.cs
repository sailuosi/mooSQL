using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.slave
{
    /// <summary>
    /// 主从功能工厂
    /// </summary>
    public class SlaveFactory
    {


        /// <summary>
        /// 创建基础底座
        /// </summary>
        /// <returns></returns>
        public static ModifyMediator createBase()
        {

            return new ModifyMediator();

        }
        /// <summary>
        /// 创建从库主控
        /// </summary>
        /// <returns></returns>
        public static SlaveTeam CreateSlave() {
            var res = new SlaveTeam();

            return res;
        }

        /// <summary>
        /// 创建从库执行器
        /// </summary>
        /// <param name="id"></param>
        /// <param name="instances"></param>
        /// <param name="onErr"></param>
        /// <returns></returns>
        public static TeamHeader createTeam(int id, List<DBInstance> instances, Func<SQLCmd, IEventEat, string> onErr = null) { 
            var head = new TeamHeader();
            head.position = id;

            foreach (DBInstance db in instances) { 
                if(db == null) continue;
                var mem = new SlaveCmdWorker();
                mem.DB=db;                
                mem.errFunction = onErr;
            }
            return head;
        }
    }
}
